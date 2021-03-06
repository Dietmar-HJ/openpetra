//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2012 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;

using GNU.Gettext;

using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Data;
using Ict.Common.Verification;

using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.MFinance.Gui.Setup;

using Ict.Petra.Shared;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.GL.Data;
using Ict.Petra.Shared.MFinance.Validation;


namespace Ict.Petra.Client.MFinance.Gui.GL
{
    public partial class TUC_GLJournals
    {
        /// <summary>
        /// Returns FMainDS
        /// </summary>
        /// <returns></returns>
        public GLBatchTDS JournalFMainDS()
        {
            return FMainDS;
        }

        /// <summary>
        /// The current active Batch number
        /// </summary>
        public Int32 FBatchNumber = -1;

        /// <summary>
        /// flags if the Journal(s) have finished loading
        /// </summary>
        public bool FJournalsLoaded = false;

        private string FLedgerBaseCurrency = string.Empty;
        private Int32 FLedgerNumber = -1;
        private ABatchRow FBatchRow = null;
        private string FBatchStatus = string.Empty;

        // Logic Objects
        private TUC_GLJournals_Cancel FCancelLogicObject = null;

        /// <summary>Sets the ledger base currency</summary>
        public string LedgerBaseCurrency
        {
            set
            {
                FLedgerBaseCurrency = value;
            }
        }

        /// <summary>
        /// WorkAroundInitialization
        /// </summary>
        public void WorkAroundInitialization()
        {
            grdDetails.DoubleClickCell += new TDoubleClickCellEventHandler(this.ShowTransactionTab);
            btnGetSetExchangeRate.Click += new EventHandler(SetExchangeRateValue);
        }

        /// <summary>
        /// Get current Journal row
        /// </summary>
        /// <returns></returns>
        public GLBatchTDSAJournalRow GetCurrentJournalRow()
        {
            return (GLBatchTDSAJournalRow) this.GetSelectedDetailRow();
        }

        /// <summary>
        /// load the journals into the grid
        /// </summary>
        /// <param name="ACurrentBatchRow"></param>
        public void LoadJournals(ABatchRow ACurrentBatchRow)
        {
            FJournalsLoaded = false;

            FBatchRow = ACurrentBatchRow;

            if (FBatchRow == null)
            {
                return;
            }

            Int32 CurrentLedgerNumber = FBatchRow.LedgerNumber;
            Int32 CurrentBatchNumber = FBatchRow.BatchNumber;
            string CurrentBatchStatus = FBatchRow.BatchStatus;
            bool BatchIsUnposted = (FBatchRow.BatchStatus == MFinanceConstants.BATCH_UNPOSTED);

            bool FirstRun = (FLedgerNumber != CurrentLedgerNumber);
            bool BatchChanged = (FBatchNumber != CurrentBatchNumber);
            bool BatchStatusChanged = (!BatchChanged && (FBatchStatus != CurrentBatchStatus));

            if (FirstRun)
            {
                FLedgerNumber = CurrentLedgerNumber;
            }

            if (BatchChanged)
            {
                FBatchNumber = CurrentBatchNumber;
            }

            if (BatchStatusChanged)
            {
                FBatchStatus = CurrentBatchStatus;
            }

            //Create object to control deletion
            FCancelLogicObject = new TUC_GLJournals_Cancel(FPetraUtilsObject, FLedgerNumber, FMainDS);

            //Make sure the current effective date for the Batch is correct
            DateTime BatchDateEffective = FBatchRow.DateEffective;

            //Check if need to load Journals
            if (!(FirstRun || BatchChanged || BatchStatusChanged))
            {
                //Need to reconnect FPrev in some circumstances
                if (FPreviouslySelectedDetailRow == null)
                {
                    DataRowView rowView = (DataRowView)grdDetails.Rows.IndexToDataSourceRow(FPrevRowChangedRow);

                    if (rowView != null)
                    {
                        FPreviouslySelectedDetailRow = (GLBatchTDSAJournalRow)(rowView.Row);
                    }
                }

                // The journals are the same and we have loaded them already
                if (BatchIsUnposted)
                {
                    if (GetSelectedRowIndex() > 0)
                    {
                        GetDetailsFromControls(GetSelectedDetailRow());
                    }
                }
            }
            else
            {
                // a different journal
                SetJournalDefaultView();
                FPreviouslySelectedDetailRow = null;

                //Load Journals
                if (FMainDS.AJournal.DefaultView.Count == 0)
                {
                    FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadAJournal(FLedgerNumber, FBatchNumber));
                }

                if (BatchIsUnposted)
                {
                    if (!dtpDetailDateEffective.Date.HasValue || (dtpDetailDateEffective.Date.Value != BatchDateEffective))
                    {
                        dtpDetailDateEffective.Date = BatchDateEffective;
                    }
                }

                //Check if DateEffective has changed and then update all
                foreach (DataRowView drv in FMainDS.AJournal.DefaultView)
                {
                    AJournalRow jr = (AJournalRow)drv.Row;

                    if (jr.DateEffective != BatchDateEffective)
                    {
                        ((TFrmGLBatch)ParentForm).GetTransactionsControl().UpdateTransactionTotals(TGLBatchEnums.eGLLevel.Batch, true);
                        break;
                    }
                }

                ShowData();

                // Now set up the complete current filter
                FFilterAndFindObject.FilterPanelControls.SetBaseFilter(FMainDS.AJournal.DefaultView.RowFilter, true);
                FFilterAndFindObject.ApplyFilter();
            }

            int nRowToSelect = 1;

            TFrmGLBatch myParentForm = (TFrmGLBatch)ParentForm;

            if (myParentForm.InitialBatchFound)
            {
                DataView myView = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).DataView;

                for (int counter = 0; (counter < myView.Count); counter++)
                {
                    int myViewJournalNumber = (int)myView[counter][AJournalTable.GetJournalNumberDBName()];

                    if (myViewJournalNumber == myParentForm.InitialJournalNumber)
                    {
                        nRowToSelect = counter + 1;
                        break;
                    }
                }
            }
            else
            {
                nRowToSelect = (BatchChanged || FirstRun) ? 1 : FPrevRowChangedRow;
            }

            //This will also call UpdateChangeableStatus
            SelectRowInGrid(nRowToSelect);

            UpdateRecordNumberDisplay();
            FFilterAndFindObject.SetRecordNumberDisplayProperties();

            txtControl.CurrencyCode = TTxtCurrencyTextBox.CURRENCY_STANDARD_2_DP;
            txtCredit.CurrencyCode = FLedgerBaseCurrency;
            txtDebit.CurrencyCode = FLedgerBaseCurrency;

            FJournalsLoaded = true;
        }

        /// <summary>
        /// Checks various things on the form before saving
        /// </summary>
        public void CheckBeforeSaving()
        {
        }

        private void SetJournalDefaultView()
        {
            string DVRowFilter = string.Format("{0}={1}",
                AJournalTable.GetBatchNumberDBName(),
                FBatchNumber);

            FMainDS.AJournal.DefaultView.RowFilter = DVRowFilter;
            FMainDS.AJournal.DefaultView.Sort = String.Format("{0} DESC",
                AJournalTable.GetJournalNumberDBName()
                );

            FFilterAndFindObject.FilterPanelControls.SetBaseFilter(DVRowFilter, true);
            FFilterAndFindObject.CurrentActiveFilter = DVRowFilter;
        }

        /// <summary>
        /// show ledger and batch number
        /// </summary>
        private void ShowDataManual()
        {
            try
            {
                FPetraUtilsObject.SuppressChangeDetection = true;

                if (FLedgerNumber != -1)
                {
                    txtLedgerNumber.Text = TFinanceControls.GetLedgerNumberAndName(FLedgerNumber);
                    txtBatchNumber.Text = FBatchNumber.ToString();
                }

                if (FPreviouslySelectedDetailRow != null)
                {
                    txtDebit.NumberValueDecimal = FPreviouslySelectedDetailRow.JournalDebitTotal;
                    txtCredit.NumberValueDecimal = FPreviouslySelectedDetailRow.JournalCreditTotal;
                    txtControl.NumberValueDecimal =
                        FPreviouslySelectedDetailRow.JournalDebitTotal -
                        FPreviouslySelectedDetailRow.JournalCreditTotal;
                }
            }
            finally
            {
                FPetraUtilsObject.SuppressChangeDetection = false;
            }
        }

        /// <summary>
        /// update the journal header fields from a batch
        /// </summary>
        /// <param name="ABatch"></param>
        public void UpdateHeaderTotals(ABatchRow ABatch)
        {
            decimal SumDebits = 0.0M;
            decimal SumCredits = 0.0M;

            DataView JournalDV = new DataView(FMainDS.AJournal);

            JournalDV.RowFilter = String.Format("{0}={1}",
                AJournalTable.GetBatchNumberDBName(),
                ABatch.BatchNumber);

            foreach (DataRowView v in JournalDV)
            {
                AJournalRow r = (AJournalRow)v.Row;

                SumCredits += r.JournalCreditTotal;
                SumDebits += r.JournalDebitTotal;
            }

            FPetraUtilsObject.DisableDataChangedEvent();

            txtDebit.NumberValueDecimal = SumDebits;
            txtCredit.NumberValueDecimal = SumCredits;
            txtControl.NumberValueDecimal = ABatch.BatchControlTotal;
            txtCurrentPeriod.Text = ABatch.BatchPeriod.ToString();

            FPetraUtilsObject.EnableDataChangedEvent();
        }

        /// <summary>
        /// Call ShowDetails() from outside form
        /// </summary>
        public void ShowDetailsRefresh()
        {
            ShowDetails();
        }

        private void ShowDetailsManual(AJournalRow ARow)
        {
            bool JournalRowIsNull = (ARow == null);

            grdDetails.TabStop = (!JournalRowIsNull);

            //Enable the transactions tab accordingly
            ((TFrmGLBatch)ParentForm).EnableTransactions(!JournalRowIsNull && (ARow.JournalStatus != MFinanceConstants.BATCH_CANCELLED));

            UpdateChangeableStatus();

            if (JournalRowIsNull)
            {
                btnAdd.Focus();
            }
            else
            {
                RefreshCurrencyAndExchangeRate();
            }
        }

        /// <summary>
        /// Re-show the specified row
        /// </summary>
        /// <param name="ACurrentBatch"></param>
        /// <param name="AJournalToCancel"></param>
        /// <param name="ARedisplay"></param>
        public void PrepareJournalDataForCancelling(Int32 ACurrentBatch, Int32 AJournalToCancel, Boolean ARedisplay)
        {
            //This code will only be called when the Journal tab is active.

            DataView JournalDV = new DataView(FMainDS.AJournal);
            DataView TransDV = new DataView(FMainDS.ATransaction);
            DataView TransAnalDV = new DataView(FMainDS.ATransAnalAttrib);

            JournalDV.RowFilter = String.Format("{0}={1} And {2}={3}",
                AJournalTable.GetBatchNumberDBName(),
                ACurrentBatch,
                AJournalTable.GetJournalNumberDBName(),
                AJournalToCancel);

            TransDV.RowFilter = String.Format("{0}={1} And {2}={3}",
                ATransactionTable.GetBatchNumberDBName(),
                ACurrentBatch,
                ATransactionTable.GetJournalNumberDBName(),
                AJournalToCancel);

            TransAnalDV.RowFilter = String.Format("{0}={1} And {2}={3}",
                ATransAnalAttribTable.GetBatchNumberDBName(),
                ACurrentBatch,
                ATransAnalAttribTable.GetJournalNumberDBName(),
                AJournalToCancel);

            //Work from lowest level up
            if (TransAnalDV.Count > 0)
            {
                TransAnalDV.Sort = String.Format("{0}, {1}",
                    ATransAnalAttribTable.GetTransactionNumberDBName(),
                    ATransAnalAttribTable.GetAnalysisTypeCodeDBName());

                foreach (DataRowView drv in TransAnalDV)
                {
                    ATransAnalAttribRow transAnalRow = (ATransAnalAttribRow)drv.Row;

                    if (transAnalRow.RowState == DataRowState.Added)
                    {
                        //Do nothing
                    }
                    else if (transAnalRow.RowState != DataRowState.Unchanged)
                    {
                        transAnalRow.RejectChanges();
                    }
                }
            }

            if (TransDV.Count > 0)
            {
                TransDV.Sort = String.Format("{0}", ATransactionTable.GetTransactionNumberDBName());

                foreach (DataRowView drv in TransDV)
                {
                    ATransactionRow transRow = (ATransactionRow)drv.Row;

                    if (transRow.RowState == DataRowState.Added)
                    {
                        //Do nothing
                    }
                    else if (transRow.RowState != DataRowState.Unchanged)
                    {
                        transRow.RejectChanges();
                    }
                }
            }

            if (JournalDV.Count > 0)
            {
                GLBatchTDSAJournalRow journalRow = (GLBatchTDSAJournalRow)JournalDV[0].Row;

                //No need to check for Added state as new journals are always saved
                // on creation
                if (journalRow.RowState != DataRowState.Unchanged)
                {
                    journalRow.RejectChanges();
                }

                if (ARedisplay)
                {
                    ShowDetails(journalRow);
                }
            }

            if (TransDV.Count == 0)
            {
                //Load all related data for journal ready to delete/cancel
                FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadATransactionAndRelatedTablesForJournal(FLedgerNumber, ACurrentBatch,
                        AJournalToCancel));
            }
        }

        private ABatchRow GetBatchRow()
        {
            return ((TFrmGLBatch)ParentForm).GetBatchControl().GetSelectedDetailRow();
        }

        /// <summary>
        /// add a new journal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void NewRow(System.Object sender, EventArgs e)
        {
            TFrmGLBatch MainForm = (TFrmGLBatch) this.ParentForm;

            if (FPetraUtilsObject.HasChanges && !MainForm.SaveChanges())
            {
                return;
            }

            FPetraUtilsObject.VerificationResultCollection.Clear();

            this.CreateNewAJournal();

            //Copy description from Batch
            txtDetailJournalDescription.Text = FBatchRow.BatchDescription;

            //Needed as GL journals can not be deleted
            MainForm.SaveChanges();
            MainForm.EnableTransactions();

            txtDetailJournalDescription.Focus();
        }

        /// <summary>
        /// make sure the correct journal number is assigned and the batch.lastJournal is updated
        /// </summary>
        /// <param name="ANewRow"></param>
        public void NewRowManual(ref GLBatchTDSAJournalRow ANewRow)
        {
            if ((ANewRow == null) || (FLedgerNumber == -1))
            {
                return;
            }

            ALedgerRow LedgerRow =
                ((ALedgerTable)TDataCache.TMFinance.GetCacheableFinanceTable(
                     TCacheableFinanceTablesEnum.LedgerDetails, FLedgerNumber))[0];

            ANewRow.LedgerNumber = FBatchRow.LedgerNumber;
            ANewRow.BatchNumber = FBatchRow.BatchNumber;
            ANewRow.JournalNumber = ++FBatchRow.LastJournal;

            // manually created journals are all GL
            ANewRow.SubSystemCode = MFinanceConstants.SUB_SYSTEM_GL;
            ANewRow.TransactionTypeCode = MFinanceConstants.STANDARD_JOURNAL;

            ANewRow.TransactionCurrency = LedgerRow.BaseCurrency;
            ANewRow.ExchangeRateToBase = 1;
            ANewRow.DateEffective = FBatchRow.DateEffective;
            ANewRow.JournalPeriod = FBatchRow.BatchPeriod;
            ANewRow.JournalDebitTotalBase = 0.0M;
            ANewRow.JournalCreditTotalBase = 0.0M;
        }

        /// Initialise some comboboxes
        private void BeforeShowDetailsManual(AJournalRow ARow)
        {
            // SubSystemCode: the user can only select GL, but the system can generate eg. AP journals or GR journals
            this.cmbDetailSubSystemCode.Items.Clear();
            this.cmbDetailSubSystemCode.Items.AddRange(new object[] { ARow.SubSystemCode });

            TFinanceControls.InitialiseTransactionTypeList(ref cmbDetailTransactionTypeCode, FLedgerNumber, ARow.SubSystemCode);
        }

        private void ShowTransactionTab(Object sender, EventArgs e)
        {
            ((TFrmGLBatch)ParentForm).SelectTab(TGLBatchEnums.eGLTabs.Transactions);
        }

        /// <summary>
        /// enable or disable the buttons
        /// </summary>
        public void UpdateChangeableStatus()
        {
            Boolean IsChangeable = ((!FPetraUtilsObject.DetailProtectedMode)
                                    && (GetBatchRow() != null)
                                    && (GetBatchRow().BatchStatus == MFinanceConstants.BATCH_UNPOSTED));

            Boolean JournalUpdatable = ((FPreviouslySelectedDetailRow != null)
                                        && (FPreviouslySelectedDetailRow.JournalStatus == MFinanceConstants.BATCH_UNPOSTED));

            //Process buttons
            this.btnCancel.Enabled = (IsChangeable && JournalUpdatable);
            this.btnAdd.Enabled = IsChangeable;

            pnlDetails.Enabled = (IsChangeable && JournalUpdatable);
            pnlDetailsProtected = !IsChangeable;

            this.btnGetSetExchangeRate.Enabled = IsChangeable && JournalUpdatable
                                                 && (FPreviouslySelectedDetailRow.TransactionCurrency != FLedgerBaseCurrency);
        }

        private void ClearControls()
        {
            FPetraUtilsObject.DisableDataChangedEvent();

            txtDetailJournalDescription.Clear();
            cmbDetailTransactionTypeCode.SelectedIndex = -1;
            cmbDetailTransactionCurrency.SelectedIndex = -1;

            FPetraUtilsObject.EnableDataChangedEvent();
        }

        /// <summary>
        /// clear the current selection
        /// </summary>
        /// <param name="ABatchToClear"></param>
        /// <param name="AResetFBatchNumber"></param>
        public void ClearCurrentSelection(int ABatchToClear = 0, bool AResetFBatchNumber = true)
        {
            //Called from Batch tab, so no need to check for GetDetailsFromControls()
            // as tab change does that and current tab is Batch tab.
            if (this.FPreviouslySelectedDetailRow == null)
            {
                return;
            }
            else if ((ABatchToClear > 0) && (FPreviouslySelectedDetailRow.BatchNumber != ABatchToClear))
            {
                return;
            }

            //Set selection to null
            this.FPreviouslySelectedDetailRow = null;

            if (AResetFBatchNumber)
            {
                FBatchNumber = -1;
            }
        }

        private void ValidateDataDetailsManual(AJournalRow ARow)
        {
            TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

            TSharedFinanceValidation_GL.ValidateGLJournalManual(this, ARow, ref VerificationResultCollection,
                FValidationControlsDict, null, null, null, FLedgerBaseCurrency);

            //TODO: remove this once database definition is set for Batch Description to be NOT NULL
            // Description is mandatory then make sure it is set
            if (txtDetailJournalDescription.Text.Length == 0)
            {
                DataColumn ValidationColumn;
                TVerificationResult VerificationResult = null;
                object ValidationContext;

                ValidationColumn = ARow.Table.Columns[AJournalTable.ColumnJournalDescriptionId];
                ValidationContext = String.Format("Batch no.: {0}, Journal no.: {1}",
                    ARow.BatchNumber,
                    ARow.JournalNumber);

                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.JournalDescription,
                    "Description of " + ValidationContext,
                    this, ValidationColumn, null);

                // Handle addition/removal to/from TVerificationResultCollection
                VerificationResultCollection.Auto_Add_Or_AddOrRemove(this, VerificationResult, ValidationColumn, true);
            }
        }

        private void SetFocusToDetailsGrid()
        {
            if ((grdDetails != null) && grdDetails.CanFocus)
            {
                grdDetails.Focus();
            }
        }

        /// <summary>
        /// Shows the Filter/Find UserControl and switches to the Find Tab.
        /// </summary>
        public void ShowFindPanel()
        {
            if (FFilterAndFindObject.FilterFindPanel == null)
            {
                FFilterAndFindObject.ToggleFilter();
            }

            FFilterAndFindObject.FilterFindPanel.DisplayFindTab();
        }

        // Fired by the currency combo box when the selected value changes
        private void CurrencyCodeChanged(object sender, EventArgs e)
        {
            if (FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null)
                || (GetBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            string NewCurrency = cmbDetailTransactionCurrency.GetSelectedString();

            if (FPreviouslySelectedDetailRow.TransactionCurrency != NewCurrency)
            {
                FPreviouslySelectedDetailRow.TransactionCurrency = NewCurrency;
                FPreviouslySelectedDetailRow.ExchangeRateToBase = (NewCurrency == FLedgerBaseCurrency) ? 1.0m : 0.0m;
                RefreshCurrencyAndExchangeRate();
            }
        }

        private void TransactionTypeCodeChanged(Object sender, EventArgs e)
        {
            if (cmbDetailTransactionTypeCode.GetSelectedString() == CommonAccountingTransactionTypesEnum.ALLOC.ToString())
            {
                btnAddAllocations.Visible = true;
                btnAddAllocations.Text = Catalog.GetString("Add Allocation");
            }
            else if (cmbDetailTransactionTypeCode.GetSelectedString() == CommonAccountingTransactionTypesEnum.REALLOC.ToString())
            {
                btnAddAllocations.Visible = true;
                btnAddAllocations.Text = Catalog.GetString("Add Reallocation");
            }
            else
            {
                btnAddAllocations.Visible = false;
            }
        }

        private void AddAllocations(Object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            if (cmbDetailTransactionTypeCode.GetSelectedString() == CommonAccountingTransactionTypesEnum.ALLOC.ToString())
            {
                TFrmAllocationJournalDialog AddAllocationJournal = new TFrmAllocationJournalDialog(this.FindForm());
                AddAllocationJournal.Journal = this.GetSelectedDetailRow();

                // open as a modal form
                if (AddAllocationJournal.ShowDialog() == DialogResult.OK)
                {
                    FMainDS.Merge(AddAllocationJournal.MainDS);

                    // manually enable save button (otherwise this doesn't happen)
                    FPetraUtilsObject.SetChangedFlag();
                }
            }
            else if (cmbDetailTransactionTypeCode.GetSelectedString() == CommonAccountingTransactionTypesEnum.REALLOC.ToString())
            {
                TFrmReallocationJournalDialog AddReallocationJournal = new TFrmReallocationJournalDialog(this.FindForm());
                AddReallocationJournal.Journal = this.GetSelectedDetailRow();

                // open as a modal form
                if (AddReallocationJournal.ShowDialog() == DialogResult.OK)
                {
                    FMainDS.Merge(AddReallocationJournal.MainDS);

                    // manually enable save button (otherwise this doesn't happen)
                    FPetraUtilsObject.SetChangedFlag();
                }
            }

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Cancel any changes made to this form
        /// </summary>
        public void CancelChangesToFixedBatches()
        {
            if ((GetBatchRow() != null) && (GetBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                DataView journalDV = new DataView(FMainDS.AJournal);
                journalDV.RowFilter = string.Format("{0}={1}",
                    AJournalTable.GetBatchNumberDBName(),
                    GetBatchRow().BatchNumber);

                foreach (DataRowView drv in journalDV)
                {
                    AJournalRow jr = (AJournalRow)drv.Row;
                    jr.RejectChanges();
                }
            }
        }

        private void CancelRow(System.Object sender, EventArgs e)
        {
            if (FPreviouslySelectedDetailRow == null)
            {
                MessageBox.Show(Catalog.GetString("Select the row to cancel first"));
                return;
            }

            TFrmGLBatch MainForm = (TFrmGLBatch) this.ParentForm;

            int CurrentlySelectedRow = grdDetails.GetFirstHighlightedRowIndex();

            try
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.CANCELLINGJOURNAL;

                if (FCancelLogicObject.CancelRow(FPreviouslySelectedDetailRow))
                {
                    UpdateHeaderTotals(FBatchRow);
                    SelectRowInGrid(CurrentlySelectedRow);
                    UpdateRecordNumberDisplay();

                    SetJournalDefaultView();
                    FFilterAndFindObject.ApplyFilter();

                    UpdateChangeableStatus();
                    MainForm.DisableTransactions();
                }
            }
            finally
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.NONE;
            }
        }

        private void RefreshCurrencyAndExchangeRate()
        {
            txtDetailExchangeRateToBase.NumberValueDecimal = FPreviouslySelectedDetailRow.ExchangeRateToBase;

            btnGetSetExchangeRate.Enabled = (FPreviouslySelectedDetailRow.TransactionCurrency != FLedgerBaseCurrency);

            if (FPreviouslySelectedDetailRow.ExchangeRateToBase > 0.0m)
            {
                ((TFrmGLBatch)ParentForm).GetTransactionsControl().UpdateTransactionTotals(TGLBatchEnums.eGLLevel.Journal);
            }
        }

        private void SetExchangeRateValue(Object sender, EventArgs e)
        {
            TFrmSetupDailyExchangeRate SetupDailyExchangeRate =
                new TFrmSetupDailyExchangeRate(FPetraUtilsObject.GetForm());

            decimal SelectedExchangeRate;
            DateTime SelectedEffectiveDate;
            int SelectedEffectiveTime;

            if (SetupDailyExchangeRate.ShowDialog(
                    FLedgerNumber,
                    dtpDetailDateEffective.Date.HasValue ? dtpDetailDateEffective.Date.Value : DateTime.Today,
                    cmbDetailTransactionCurrency.GetSelectedString(),
                    (txtDetailExchangeRateToBase.NumberValueDecimal == null) ? 0.0m : txtDetailExchangeRateToBase.NumberValueDecimal.Value,
                    out SelectedExchangeRate,
                    out SelectedEffectiveDate,
                    out SelectedEffectiveTime) == DialogResult.Cancel)
            {
                return;
            }

            if (FPreviouslySelectedDetailRow.ExchangeRateToBase != SelectedExchangeRate)
            {
                FPreviouslySelectedDetailRow.ExchangeRateToBase = SelectedExchangeRate;
                //Enforce save needed condition
                FPetraUtilsObject.SetChangedFlag();
            }

            txtDetailExchangeRateToBase.NumberValueDecimal = SelectedExchangeRate;
            FPreviouslySelectedDetailRow.ExchangeRateTime = SelectedEffectiveTime;

            RefreshCurrencyAndExchangeRate();
        }

        /// <summary>
        /// Update the effective date from outside
        /// </summary>
        /// <param name="AEffectiveDate"></param>
        public void UpdateEffectiveDateForCurrentRow(DateTime AEffectiveDate)
        {
            if ((GetSelectedDetailRow() != null) && (GetBatchRow().BatchStatus == MFinanceConstants.BATCH_UNPOSTED))
            {
                GetSelectedDetailRow().DateEffective = AEffectiveDate;
                dtpDetailDateEffective.Date = AEffectiveDate;
                GetDetailsFromControls(GetSelectedDetailRow());

                // reset exchange rate
                FPreviouslySelectedDetailRow.ExchangeRateToBase =
                    (FPreviouslySelectedDetailRow.TransactionCurrency == FLedgerBaseCurrency) ? 1.0m : 0.0m;

                RefreshCurrencyAndExchangeRate();
            }
        }

        /// <summary>
        /// This event is fired when there is a currency change that 'sticks' for more than 1 second.
        /// We use it to see if the server has a specific rate for this currency and date
        /// </summary>
        private void StickyCurrencyChange(object sender, EventArgs e)
        {
            if (FPreviouslySelectedDetailRow.TransactionCurrency == FLedgerBaseCurrency)
            {
                return;
            }

            decimal suggestedRate = 0.0m;

            try
            {
                FPetraUtilsObject.GetForm().Cursor = Cursors.WaitCursor;

                if (dtpDetailDateEffective.Date.HasValue)
                {
                    // get a specific single rate for the specific date
                    suggestedRate = TRemote.MFinance.GL.WebConnectors.GetDailyExchangeRate(
                        FPreviouslySelectedDetailRow.TransactionCurrency, FLedgerBaseCurrency, dtpDetailDateEffective.Date.Value, 0, true);
                }
            }
            finally
            {
                FPetraUtilsObject.GetForm().Cursor = Cursors.Default;
            }

            // Is it different??
            if (FPreviouslySelectedDetailRow.ExchangeRateToBase != suggestedRate)
            {
                FPreviouslySelectedDetailRow.ExchangeRateToBase = suggestedRate;
                CurrencyCodeChanged(null, null);
            }
        }
    }
}