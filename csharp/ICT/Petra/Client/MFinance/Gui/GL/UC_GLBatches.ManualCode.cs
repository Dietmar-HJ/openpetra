//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop, alanP
//
// Copyright 2004-2014 by OM International
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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Ict.Common;
using Ict.Common.Data;
using Ict.Common.Controls;
using Ict.Common.Verification;

using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Client.MCommon;

using Ict.Petra.Shared;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.Validation;
using Ict.Petra.Shared.Security;

namespace Ict.Petra.Client.MFinance.Gui.GL
{
    /// <summary>
    /// Interface used by logic objects in order to access selected public methods in the TUC_GLBatches class
    /// </summary>
    public interface IUC_GLBatches
    {
        /// <summary>
        /// Load the batches for the current financial year (used in particular when the screen starts up).
        /// </summary>
        void UpdateDisplay();

        /// <summary>
        /// Reload the batches
        /// </summary>
        void ReloadBatches(bool AIsFromMessage = false);

        /// <summary>
        /// Create a New Batch
        /// </summary>
        bool CreateNewABatch();
    }

    public partial class TUC_GLBatches : IUC_GLBatches
    {
        private Int32 FLedgerNumber = -1;

        // Logic objects
        private TUC_GLBatches_LoadAndFilter FLoadAndFilterLogicObject = null;
        private TUC_GLBatches_Import FImportLogicObject = null;
        private TUC_GLBatches_Cancel FCancelLogicObject = null;
        private TUC_GLBatches_Post FPostLogicObject = null;
        private TUC_GLBatches_Reverse FReverseLogicObject = null;

        private DateTime FDefaultDate;
        private bool FBatchesLoaded = false;

        //Date related
        private DateTime FStartDateCurrentPeriod;
        private DateTime FEndDateLastForwardingPeriod;
        private DateTime FCurrentEffectiveDate;
        private int FCurrentLedgerYear;
        private int FCurrentLedgerPeriod;

        /// <summary>
        /// InactiveValuesWarningOnGLPosting User Setting
        /// </summary>
        public bool FInactiveValuesWarningOnGLPosting = false;

        /// <summary>
        ///List of all batches and whether or not the user has been warned of the presence
        /// of inactive fields on saving.
        /// </summary>
        public Dictionary <int, bool>FUnpostedBatchesVerifiedOnSavingDict = new Dictionary <int, bool>();

        /// <summary>
        /// load the batches into the grid
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;

                InitialiseLogicObjects();
                InitialiseLedgerControls();
            }
        }

        private void InitialiseLogicObjects()
        {
            FLoadAndFilterLogicObject = new TUC_GLBatches_LoadAndFilter(FPetraUtilsObject, FLedgerNumber, FMainDS, FFilterAndFindObject);
            FImportLogicObject = new TUC_GLBatches_Import(FPetraUtilsObject, FLedgerNumber, FMainDS, this);
            FCancelLogicObject = new TUC_GLBatches_Cancel(FPetraUtilsObject, FLedgerNumber, FMainDS);
            FPostLogicObject = new TUC_GLBatches_Post(FPetraUtilsObject, FLedgerNumber, FMainDS, this);
            FReverseLogicObject = new TUC_GLBatches_Reverse(FPetraUtilsObject, FLedgerNumber, FMainDS, this);
        }

        private void InitialiseLedgerControls()
        {
            //Set the valid date range label
            TLedgerSelection.GetCurrentPostingRangeDates(FLedgerNumber,
                out FStartDateCurrentPeriod,
                out FEndDateLastForwardingPeriod,
                out FDefaultDate);

            lblValidDateRange.Text = String.Format(Catalog.GetString("Valid between {0} and {1}"),
                StringHelper.DateToLocalizedString(FStartDateCurrentPeriod, false, false),
                StringHelper.DateToLocalizedString(FEndDateLastForwardingPeriod, false, false));

            // Get the current year/period and pass on to the filter logic object
            ALedgerRow LedgerRow =
                ((ALedgerTable)TDataCache.TMFinance.GetCacheableFinanceTable(TCacheableFinanceTablesEnum.LedgerDetails, FLedgerNumber))[0];

            FCurrentLedgerYear = LedgerRow.CurrentFinancialYear;
            FCurrentLedgerPeriod = LedgerRow.CurrentPeriod;
            FLoadAndFilterLogicObject.CurrentLedgerYear = FCurrentLedgerYear;
            FLoadAndFilterLogicObject.CurrentLedgerPeriod = FCurrentLedgerPeriod;

            TFrmGLBatch myParentForm = (TFrmGLBatch) this.ParentForm;
            myParentForm.GetJournalsControl().LedgerBaseCurrency = LedgerRow.BaseCurrency;
        }

        /// <summary>
        /// load the batches into the grid
        /// </summary>
        public void LoadBatchesForCurrentYear()
        {
            FBatchesLoaded = false;

            TFrmGLBatch myParentForm = (TFrmGLBatch) this.ParentForm;
            myParentForm.InitialBatchFound = false;

            bool performStandardLoad = true;

            if (myParentForm.InitialBatchYear >= 0)
            {
                FLoadAndFilterLogicObject.StatusAll = true;

                int yearIndex = FLoadAndFilterLogicObject.FindYearAsIndex(myParentForm.InitialBatchYear);

                if (yearIndex >= 0)
                {
                    FLoadAndFilterLogicObject.YearIndex = yearIndex;

                    if (myParentForm.InitialBatchPeriod >= 0)
                    {
                        FLoadAndFilterLogicObject.PeriodIndex = FLoadAndFilterLogicObject.FindPeriodAsIndex(myParentForm.InitialBatchPeriod);
                    }
                    else
                    {
                        FLoadAndFilterLogicObject.PeriodIndex = (myParentForm.InitialBatchYear == FMainDS.ALedger[0].CurrentFinancialYear) ? 1 : 0;
                    }

                    performStandardLoad = false;
                }

                // Reset the start-up value
                myParentForm.InitialBatchYear = -1;
            }

            if (performStandardLoad)
            {
                // Set up for current year with current and forwarding periods (on initial load this will already be set so will not fire a change)
                FLoadAndFilterLogicObject.YearIndex = 0;
                FLoadAndFilterLogicObject.PeriodIndex = 0;
            }

            // This call will get the first year's data and update the display.
            // Note: If the first year data has already been loaded once there will be no trip to the server to get any updates.
            //        if you know updates are available, you need to merge them afterwards or clear the data table first
            UpdateDisplay();

            if (myParentForm.LoadForImport)
            {
                // We have been launched from the Import Batches main menu screen as opposed to the regular GL Batches menu
                // Call the logic object to import:  this will request a CSV file and merge the batches on the server.
                // Finally it will call back to ReloadBatches() in this class, which merges the server data into FMainDS and selects the first row
                FImportLogicObject.ImportBatches(TUC_GLBatches_Import.TImportDataSourceEnum.FromFile);

                // Reset the flag
                myParentForm.LoadForImport = false;
            }

            FBatchesLoaded = true;
        }

        /// <summary>
        /// Enable the transaction tab
        /// </summary>
        public void AutoEnableTransTabForBatch()
        {
            bool EnableTransTab = ((FPreviouslySelectedDetailRow != null)
                                   && (FPreviouslySelectedDetailRow.LastJournal == 1)
                                   && (FPreviouslySelectedDetailRow.BatchStatus != MFinanceConstants.BATCH_CANCELLED));

            ((TFrmGLBatch) this.ParentForm).EnableTransactions(EnableTransTab);
        }

        private void LoadJournalsForCurrentBatch()
        {
            if (FPreviouslySelectedDetailRow == null)
            {
                return;
            }

            //Current Batch number
            int BatchNumber = FPreviouslySelectedDetailRow.BatchNumber;
            string BatchStatus = FPreviouslySelectedDetailRow.BatchStatus;

            if (FMainDS.AJournal != null)
            {
                FMainDS.AJournal.DefaultView.RowFilter = String.Format("{0}={1}",
                    AJournalTable.GetBatchNumberDBName(),
                    BatchNumber);

                if (FMainDS.AJournal.DefaultView.Count == 0)
                {
                    ((TFrmGLBatch) this.ParentForm).LoadJournals(FPreviouslySelectedDetailRow);
                }
            }
        }

        private void ShowDataManual()
        {
            if (FLedgerNumber == -1)
            {
                EnableButtonControl(false);
            }
        }

        /// <summary>
        /// Call ShowDetails() from outside form
        /// </summary>
        public void ShowDetailsRefresh()
        {
            ShowDetails();
        }

        /// reset the control
        public void ClearCurrentSelection()
        {
            if (this.FPreviouslySelectedDetailRow == null)
            {
                return;
            }

            if (FPetraUtilsObject.HasChanges)
            {
                GetDataFromControls();
            }

            this.FPreviouslySelectedDetailRow = null;
            ShowData();
        }

        private void UpdateChangeableStatus()
        {
            FPetraUtilsObject.EnableAction("actReverseBatch", (FPreviouslySelectedDetailRow != null)
                && FPreviouslySelectedDetailRow.BatchStatus == MFinanceConstants.BATCH_POSTED);

            Boolean Postable = (FPreviouslySelectedDetailRow != null)
                               && FPreviouslySelectedDetailRow.BatchStatus == MFinanceConstants.BATCH_UNPOSTED;

            FPetraUtilsObject.EnableAction("actPostBatch", Postable);
            FPetraUtilsObject.EnableAction("actTestPostBatch", Postable);
            FPetraUtilsObject.EnableAction("actCancel", Postable);
            pnlDetails.Enabled = Postable;
            pnlDetailsProtected = !Postable;

            if ((FPreviouslySelectedDetailRow == null) && (((TFrmGLBatch) this.ParentForm) != null))
            {
                ((TFrmGLBatch) this.ParentForm).DisableJournals();
            }
        }

        /// <summary>
        /// Checks various things on the form before saving
        /// </summary>
        public void CheckBeforeSaving()
        {
            UpdateUnpostedBatchDictionary();
        }

        /// <summary>
        /// Update the dictionary that stores all unposted batches
        ///  and whether or not they have been warned about inactive
        ///   fields
        /// </summary>
        /// <param name="ABatchNumberToExclude"></param>
        public void UpdateUnpostedBatchDictionary(int ABatchNumberToExclude = 0)
        {
            if (ABatchNumberToExclude > 0)
            {
                FUnpostedBatchesVerifiedOnSavingDict.Remove(ABatchNumberToExclude);
            }

            DataView BatchDV = new DataView(FMainDS.ABatch);

            //Just want unposted batches
            BatchDV.RowFilter = string.Format("{0}='{1}'",
                ABatchTable.GetBatchStatusDBName(),
                MFinanceConstants.BATCH_UNPOSTED);

            foreach (DataRowView bRV in BatchDV)
            {
                ABatchRow br = (ABatchRow)bRV.Row;

                int currentBatch = br.BatchNumber;

                if ((currentBatch != ABatchNumberToExclude) && !FUnpostedBatchesVerifiedOnSavingDict.ContainsKey(currentBatch))
                {
                    FUnpostedBatchesVerifiedOnSavingDict.Add(br.BatchNumber, false);
                }
            }
        }

        private void ValidateDataDetailsManual(ABatchRow ARow)
        {
            if ((ARow == null) || (ARow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

            ParseHashTotal(ARow);

            TSharedFinanceValidation_GL.ValidateGLBatchManual(this,
                ARow,
                ref VerificationResultCollection,
                FValidationControlsDict,
                FStartDateCurrentPeriod,
                FEndDateLastForwardingPeriod);

            //TODO: remove this once database definition is set for Batch Description to be NOT NULL
            // Description is mandatory then make sure it is set
            if (txtDetailBatchDescription.Text.Length == 0)
            {
                DataColumn ValidationColumn;
                TVerificationResult VerificationResult = null;
                object ValidationContext;

                ValidationColumn = ARow.Table.Columns[ABatchTable.ColumnBatchDescriptionId];
                ValidationContext = String.Format("Batch number {0}",
                    ARow.BatchNumber);

                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.BatchDescription,
                    "Description of " + ValidationContext,
                    this, ValidationColumn, null);

                // Handle addition/removal to/from TVerificationResultCollection
                VerificationResultCollection.Auto_Add_Or_AddOrRemove(this, VerificationResult, ValidationColumn, true);
            }
        }

        private void ParseHashTotal(ABatchRow ARow)
        {
            if (ARow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED)
            {
                return;
            }

            if ((txtDetailBatchControlTotal.NumberValueDecimal == null) || !txtDetailBatchControlTotal.NumberValueDecimal.HasValue)
            {
                bool prev = FPetraUtilsObject.SuppressChangeDetection;
                FPetraUtilsObject.SuppressChangeDetection = true;
                txtDetailBatchControlTotal.NumberValueDecimal = 0m;
                FPetraUtilsObject.SuppressChangeDetection = prev;
            }

            if (ARow.BatchControlTotal != txtDetailBatchControlTotal.NumberValueDecimal.Value)
            {
                ARow.BatchControlTotal = txtDetailBatchControlTotal.NumberValueDecimal.Value;
            }
        }

        private void ShowDetailsManual(ABatchRow ARow)
        {
            AutoEnableTransTabForBatch();
            grdDetails.TabStop = (ARow != null);

            if (ARow == null)
            {
                pnlDetails.Enabled = false;
                ((TFrmGLBatch) this.ParentForm).DisableJournals();
                ((TFrmGLBatch) this.ParentForm).DisableTransactions();
                EnableButtonControl(false);
                ClearDetailControls();
                return;
            }

            FPetraUtilsObject.DetailProtectedMode =
                (ARow.BatchStatus.Equals(MFinanceConstants.BATCH_POSTED)
                 || ARow.BatchStatus.Equals(MFinanceConstants.BATCH_CANCELLED));

            FCurrentEffectiveDate = ARow.DateEffective;

            UpdateBatchPeriod(null, null);

            UpdateChangeableStatus();
            ((TFrmGLBatch) this.ParentForm).EnableJournals();
        }

        /// <summary>
        /// This routine is called by a double click on a batch row, which means: Open the
        /// Journal Tab of this batch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowJournalTab(Object sender, EventArgs e)
        {
            ((TFrmGLBatch)ParentForm).SelectTab(TGLBatchEnums.eGLTabs.Journals);
        }

        /// <summary>
        /// Controls the enabled status of the Cancel, Test and Post buttons
        /// </summary>
        /// <param name="AEnable"></param>
        private void EnableButtonControl(bool AEnable)
        {
            if (AEnable)
            {
                if (!pnlDetails.Enabled)
                {
                    pnlDetails.Enabled = true;
                }
            }

            btnPostBatch.Enabled = AEnable;
            btnTestPostBatch.Enabled = AEnable;
            btnCancel.Enabled = AEnable;
        }

        /// <summary>
        /// Undo all changes to the specified batch ready to cancel it.
        ///  This avoids unecessary validation errors when cancelling.
        /// </summary>
        /// <param name="ABatchToCancel"></param>
        /// <param name="ARedisplay"></param>
        public void PrepareBatchDataForCancelling(Int32 ABatchToCancel, Boolean ARedisplay)
        {
            //This code will only be called when the Batch tab is active.

            DataView GLBatchDV = new DataView(FMainDS.ABatch);
            DataView JournalDV = new DataView(FMainDS.AJournal);
            DataView TransDV = new DataView(FMainDS.ATransaction);
            DataView TransAnalDV = new DataView(FMainDS.ATransAnalAttrib);

            GLBatchDV.RowFilter = String.Format("{0}={1}",
                ABatchTable.GetBatchNumberDBName(),
                ABatchToCancel);

            JournalDV.RowFilter = String.Format("{0}={1}",
                AJournalTable.GetBatchNumberDBName(),
                ABatchToCancel);

            TransDV.RowFilter = String.Format("{0}={1}",
                ATransactionTable.GetBatchNumberDBName(),
                ABatchToCancel);

            TransAnalDV.RowFilter = String.Format("{0}={1}",
                ATransAnalAttribTable.GetBatchNumberDBName(),
                ABatchToCancel);

            //Work from lowest level up
            if (TransAnalDV.Count > 0)
            {
                TransAnalDV.Sort = String.Format("{0}, {1}, {2}",
                    ATransAnalAttribTable.GetJournalNumberDBName(),
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
                TransDV.Sort = String.Format("{0}, {1}",
                    ATransactionTable.GetJournalNumberDBName(),
                    ATransactionTable.GetTransactionNumberDBName());

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
                JournalDV.Sort = String.Format("{0}", AJournalTable.GetJournalNumberDBName());

                foreach (DataRowView drv in JournalDV)
                {
                    AJournalRow journalRow = (AJournalRow)drv.Row;

                    if (journalRow.RowState == DataRowState.Added)
                    {
                        //Do nothing
                    }
                    else if (journalRow.RowState != DataRowState.Unchanged)
                    {
                        journalRow.RejectChanges();
                    }
                }
            }

            if (GLBatchDV.Count > 0)
            {
                ABatchRow batchRow = (ABatchRow)GLBatchDV[0].Row;

                //No need to check for Added state as new batches are always saved
                // on creation

                if (batchRow.RowState != DataRowState.Unchanged)
                {
                    batchRow.RejectChanges();
                }

                if (ARedisplay)
                {
                    ShowDetails(batchRow);
                }
            }

            if (TransDV.Count == 0)
            {
                //Load all related data for batch ready to delete/cancel
                FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadAJournalAndRelatedTablesForBatch(FLedgerNumber, ABatchToCancel));
            }
        }

        /// <summary>
        /// add a new batch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewRow(System.Object sender, EventArgs e)
        {
            if (!SaveOutstandingChanges())
            {
                return;
            }

            if (CreateNewABatch())
            {
                if (!EnsureNewBatchIsVisible())
                {
                    return;
                }

                pnlDetails.Enabled = true;
                EnableButtonControl(true);

                // NOTE: we need to suppress change detection here because otherwise the DateChanged event fires off
                //   It would not normally be a problem although it can give strange effects of focussing the date box AND the description (!)
                //   and also may sometimes give problems with running some tests.
                //  So do not remove change detection suppression so we do not run UpdateBatchPeriod()
                FPetraUtilsObject.SuppressChangeDetection = true;
                FPreviouslySelectedDetailRow.DateEffective = FDefaultDate;
                dtpDetailDateEffective.Date = FDefaultDate;
                FPetraUtilsObject.SuppressChangeDetection = false;

                Int32 yearNumber = 0;
                Int32 periodNumber = 0;

                if (GetAccountingYearPeriodByDate(FLedgerNumber, FDefaultDate, out yearNumber, out periodNumber))
                {
                    FPreviouslySelectedDetailRow.BatchPeriod = periodNumber;
                }

                UpdateRecordNumberDisplay();

                //Needed as GL batches can not be deleted
                ((TFrmGLBatch)ParentForm).SaveChanges();

                //Enable the Journals if not already enabled
                ((TFrmGLBatch)ParentForm).EnableJournals();
            }
        }

        private bool GetAccountingYearPeriodByDate(Int32 ALedgerNumber, DateTime ADate, out Int32 AYear, out Int32 APeriod)
        {
            return TRemote.MFinance.GL.WebConnectors.GetAccountingYearPeriodByDate(ALedgerNumber, ADate, out AYear, out APeriod);
        }

        private void ClearControls()
        {
            try
            {
                FPetraUtilsObject.DisableDataChangedEvent();
                txtDetailBatchDescription.Clear();
                txtDetailBatchControlTotal.NumberValueDecimal = 0;
            }
            finally
            {
                FPetraUtilsObject.EnableDataChangedEvent();
            }
        }

        private void ClearDetailControls()
        {
            FPetraUtilsObject.SuppressChangeDetection = true;
            txtDetailBatchDescription.Text = string.Empty;
            txtDetailBatchControlTotal.NumberValueDecimal = 0;
            dtpDetailDateEffective.Date = FDefaultDate;
            FPetraUtilsObject.SuppressChangeDetection = false;
        }

        private int GetDataTableRowIndexByPrimaryKeys(int ALedgerNumber, int ABatchNumber)
        {
            int rowPos = 0;
            bool batchFound = false;

            foreach (DataRowView rowView in FMainDS.ABatch.DefaultView)
            {
                ABatchRow row = (ABatchRow)rowView.Row;

                if ((row.LedgerNumber == ALedgerNumber) && (row.BatchNumber == ABatchNumber))
                {
                    batchFound = true;
                    break;
                }

                rowPos++;
            }

            if (!batchFound)
            {
                rowPos = 0;
            }

            //remember grid is out of sync with DataView by 1 because of grid header rows
            return rowPos + 1;
        }

        /// <summary>
        /// Sets the initial focus to the grid or the New button depending on the row count
        /// </summary>
        public void SetInitialFocus()
        {
            if (grdDetails.CanFocus)
            {
                if ((grdDetails.Rows.Count <= 1) && btnNew.CanFocus)
                {
                    btnNew.Focus();
                }
                else
                {
                    grdDetails.Focus();
                }
            }
        }

        private void RunOnceOnParentActivationManual()
        {
            try
            {
                ParentForm.Cursor = Cursors.WaitCursor;

                grdDetails.DoubleClickCell += new TDoubleClickCellEventHandler(this.ShowJournalTab);
                grdDetails.DataSource.ListChanged += new System.ComponentModel.ListChangedEventHandler(DataSource_ListChanged);

                LoadBatchesForCurrentYear();

                txtDetailBatchControlTotal.CurrencyCode = TTxtCurrencyTextBox.CURRENCY_STANDARD_2_DP;

                SetInitialFocus();

                // Select the Journal tab if the screen opener specified a Journal Number
                TFrmGLBatch myParentForm = (TFrmGLBatch)ParentForm;

                if (myParentForm.InitialBatchFound && (myParentForm.InitialJournalNumber != -1))
                {
                    myParentForm.SelectTab(TGLBatchEnums.eGLTabs.Journals);
                }

                FInactiveValuesWarningOnGLPosting = TUserDefaults.GetBooleanDefault(TUserDefaults.FINANCE_GL_WARN_OF_INACTIVE_VALUES_ON_POSTING,
                    true);
            }
            finally
            {
                ParentForm.Cursor = Cursors.Default;
            }
        }

        private void DataSource_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if (grdDetails.CanFocus && (grdDetails.Rows.Count > 1))
            {
                grdDetails.AutoResizeGrid();
            }
        }

        private void ImportFromSpreadSheet(object sender, EventArgs e)
        {
            string CSVDataFileName;
            DateTime LatestTransactionDate;

            if (FImportLogicObject.ImportFromSpreadsheet(out CSVDataFileName, out LatestTransactionDate))
            {
                dtpDetailDateEffective.Date = LatestTransactionDate;
                txtDetailBatchDescription.Text = Path.GetFileNameWithoutExtension(CSVDataFileName);
            }
        }

        /// <summary>
        /// ImportBatches called from button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ImportBatchesFromFile(object sender, EventArgs e)
        {
            if (!SaveOutstandingChanges())
            {
                return;
            }

            FImportLogicObject.ImportBatches(TUC_GLBatches_Import.TImportDataSourceEnum.FromFile);
        }

        private void ImportBatchesFromClipboard(object sender, EventArgs e)
        {
            if (!SaveOutstandingChanges())
            {
                return;
            }

            FImportLogicObject.ImportBatches(TUC_GLBatches_Import.TImportDataSourceEnum.FromClipboard);
        }

        /// <summary>
        /// Public method called from the transactions tab
        /// </summary>
        public void ImportTransactions(TUC_GLBatches_Import.TImportDataSourceEnum AImportDataSource)
        {
            FImportLogicObject.ImportTransactions(FPreviouslySelectedDetailRow, GetCurrentJournalRow(), AImportDataSource);
        }

        private void ExportBatches(object sender, EventArgs e)
        {
            if (FPetraUtilsObject.HasChanges && !((TFrmGLBatch) this.ParentForm).SaveChanges())
            {
                // saving failed, therefore do not try to post
                MessageBox.Show(Catalog.GetString("Please correct and save changed data before the export!"),
                    Catalog.GetString("Export Error"));
                return;
            }

            TFrmGLBatchExport gl = new TFrmGLBatchExport(FPetraUtilsObject.GetForm());
            gl.LedgerNumber = FLedgerNumber;
            gl.MainDS = FMainDS;
            gl.Show();
        }

        private void CreateFilterFindPanelsManual()
        {
            ((Label)FFilterAndFindObject.FindPanelControls.FindControlByName("lblBatchNumber")).Text = "Batch number";
        }

        /// <summary>
        /// Get current Batch row
        /// </summary>
        /// <returns></returns>
        public ABatchRow GetCurrentBatchRow()
        {
            return (ABatchRow) this.GetSelectedDetailRow();
        }

        private AJournalRow GetCurrentJournalRow()
        {
            return (AJournalRow)((TFrmGLBatch) this.ParentForm).GetJournalsControl().GetSelectedDetailRow();
        }

        /// <summary>
        /// Reload batches after an import
        /// </summary>
        public void ReloadBatches(bool AIsFromMessage = false)
        {
            try
            {
                FPetraUtilsObject.GetForm().Cursor = Cursors.WaitCursor;

                if (!AIsFromMessage)
                {
                    // Before we re-load make a note of the 'last' batch number so we can work out which batches have been imported.
                    DataView dv = new DataView(FMainDS.ABatch, String.Empty, String.Format("{0} DESC",
                            ABatchTable.GetBatchNumberDBName()), DataViewRowState.CurrentRows);
                    int lastBatchNumber = (dv.Count == 0) ? 0 : ((ABatchRow)dv[0].Row).BatchNumber;

                    // Merge the new batches into our data set
                    FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadABatch(FLedgerNumber, FCurrentLedgerYear, 0));

                    // Go round each imported batch loading its journals
                    // Start with the highest batch number and continue until we reach the 'old' last batch
                    for (int i = 0; i < dv.Count; i++)
                    {
                        ABatchRow batchRow = (ABatchRow)dv[i].Row;
                        int batchNumber = batchRow.BatchNumber;

                        if (batchNumber <= lastBatchNumber)
                        {
                            break;
                        }

                        batchRow.SetModified();

                        FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadAJournalAndRelatedTablesForBatch(FLedgerNumber, batchNumber));
                    }

                    EnsureNewBatchIsVisible();
                }
                else
                {
                    if (FPetraUtilsObject.HasChanges && !((TFrmGLBatch)ParentForm).SaveChanges())
                    {
                        string msg = String.Format(Catalog.GetString("A validation error has occured on the GL Batches" +
                                " form while trying to refresh.{0}{0}" +
                                "You will need to close and reopen the GL Batches form to see the new batch" +
                                " after you have fixed the validation error."),
                            Environment.NewLine);

                        MessageBox.Show(msg, "Refresh GL Batches");
                        return;
                    }

                    FMainDS.Merge(TRemote.MFinance.GL.WebConnectors.LoadABatch(FLedgerNumber, FCurrentLedgerYear, 0));
                    grdDetails.SelectRowInGrid(1);
                }
            }
            finally
            {
                FPetraUtilsObject.GetForm().Cursor = Cursors.Default;
            }
        }

        private bool SaveOutstandingChanges()
        {
            bool RetVal = true;

            try
            {
                if (FPetraUtilsObject.HasChanges && !((TFrmGLBatch) this.ParentForm).SaveChanges())
                {
                    RetVal = false;
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }

            return RetVal;
        }

        private bool EnsureNewBatchIsVisible()
        {
            // Can we see the new row, bearing in mind we have filtering that the standard filter code does not know about?
            DataView dv = ((DevAge.ComponentModel.BoundDataView)grdDetails.DataSource).DataView;
            Int32 RowNumberGrid = DataUtilities.GetDataViewIndexByDataTableIndex(dv, FMainDS.ABatch, FMainDS.ABatch.Rows.Count - 1) + 1;

            if (RowNumberGrid < 1)
            {
                MessageBox.Show(
                    Catalog.GetString(
                        "The new row has been added but the filter may be preventing it from being displayed. The filter will be reset."),
                    Catalog.GetString("New Batch"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (!FLoadAndFilterLogicObject.StatusEditing)
                {
                    FLoadAndFilterLogicObject.StatusEditing = true;
                }

                //Set year and period to correct value
                FLoadAndFilterLogicObject.YearIndex = 0;
                FLoadAndFilterLogicObject.PeriodIndex = 0;

                FFilterAndFindObject.FilterPanelControls.ClearAllDiscretionaryFilters();

                if (SelectDetailRowByDataTableIndex(FMainDS.ABatch.Rows.Count - 1))
                {
                    // Good - we found the row so now we need to do the other stuff to the new record
                    txtDetailBatchDescription.Text = MCommonResourcestrings.StrPleaseEnterDescription;
                    txtDetailBatchDescription.Focus();
                }
                else
                {
                    // This is not supposed to happen!!
                    MessageBox.Show(
                        Catalog.GetString(
                            "The filter was reset but unexpectedly the new batch is not in the list. Please close the screen and do not save changes."),
                        Catalog.GetString("New Batch"),
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }

            return true;
        }

        private void ApplyFilterManual(ref string AFilterString)
        {
            if (FLoadAndFilterLogicObject != null)
            {
                FLoadAndFilterLogicObject.ApplyFilterManual(ref AFilterString);
            }
        }

        /// <summary>
        /// Updates the data display.  Call this after the DataSet has changed.
        /// </summary>
        public void UpdateDisplay()
        {
            TFrmGLBatch myParentForm = (TFrmGLBatch)ParentForm;
            Cursor prevCursor = myParentForm.Cursor;

            try
            {
                myParentForm.Cursor = Cursors.WaitCursor;

                // Remember our current row position
                int nCurrentRowIndex = GetSelectedRowIndex();

                // This single call will fire the event that loads data and populates the grid
                FFilterAndFindObject.ApplyFilter();

                // Did the start-up specify a batch to select?
                if (myParentForm.InitialBatchNumber > 0)
                {
                    nCurrentRowIndex = 1;
                    string filter = String.Format("{0}={1}", ABatchTable.GetBatchNumberDBName(), myParentForm.InitialBatchNumber);
                    DataView dv = new DataView(FMainDS.ABatch, filter, "", DataViewRowState.CurrentRows);

                    if (dv.Count > 0)
                    {
                        int rowToSelect = grdDetails.DataSourceRowToIndex2(dv[0].Row) + 1;

                        if (rowToSelect > 0)
                        {
                            nCurrentRowIndex = rowToSelect;
                            myParentForm.InitialBatchFound = true;
                        }
                    }

                    // Reset the start-up value
                    myParentForm.InitialBatchNumber = -1;
                }

                // Now we can select the row index we had before (if it exists)
                SelectRowInGrid(nCurrentRowIndex);
                UpdateRecordNumberDisplay();
            }
            finally
            {
                myParentForm.Cursor = prevCursor;
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

            try
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.CANCELLING;

                int currentlySelectedRow = grdDetails.GetFirstHighlightedRowIndex();

                if (FCancelLogicObject.CancelBatch(FPreviouslySelectedDetailRow))
                {
                    //Reset row to fire events
                    SelectRowInGrid(currentlySelectedRow);
                    UpdateRecordNumberDisplay();

                    //If no row exists in current view after cancellation
                    if (grdDetails.Rows.Count < 2)
                    {
                        EnableButtonControl(false);
                        ClearDetailControls();
                    }
                }
            }
            finally
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.NONE;
            }
        }

        private void UpdateBatchPeriod(object sender, EventArgs e)
        {
            if ((FPetraUtilsObject == null) || FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null))
            {
                return;
            }

            bool UpdateTransactionDates = false;

            Int32 PeriodNumber = 0;
            Int32 YearNumber = 0;
            string EffectiveDateString = string.Empty;
            DateTime EffectiveDateValue;

            try
            {
                bool rowDataHasChanged = false;

                EffectiveDateString = dtpDetailDateEffective.Date.ToString();

                if (DateTime.TryParse(EffectiveDateString, out EffectiveDateValue))
                {
                    if ((EffectiveDateValue == FCurrentEffectiveDate)
                        || (EffectiveDateValue < FStartDateCurrentPeriod)
                        || (EffectiveDateValue > FEndDateLastForwardingPeriod))
                    {
                        return;
                    }

                    //GetDetailsFromControls will do this automatically if the user tabs
                    //  passed the last control, but not if they click on another control
                    FCurrentEffectiveDate = EffectiveDateValue;

                    if (FPreviouslySelectedDetailRow.DateEffective != EffectiveDateValue)
                    {
                        FPreviouslySelectedDetailRow.DateEffective = EffectiveDateValue;
                        rowDataHasChanged = true;
                    }

                    //Check if new date is in a different Batch period to the current one
                    if (GetAccountingYearPeriodByDate(FLedgerNumber, EffectiveDateValue, out YearNumber, out PeriodNumber))
                    {
                        if (FPreviouslySelectedDetailRow.BatchPeriod != PeriodNumber)
                        {
                            FPreviouslySelectedDetailRow.BatchPeriod = PeriodNumber;
                            rowDataHasChanged = true;

                            //Update the Transaction effective dates
                            UpdateTransactionDates = true;

                            if (FLoadAndFilterLogicObject.YearIndex != 0)
                            {
                                FLoadAndFilterLogicObject.YearIndex = 0;
                                FLoadAndFilterLogicObject.PeriodIndex = 1;
                                dtpDetailDateEffective.Date = EffectiveDateValue;
                                dtpDetailDateEffective.Focus();
                            }
                            else if (FLoadAndFilterLogicObject.PeriodIndex != 1)
                            {
                                FLoadAndFilterLogicObject.PeriodIndex = 1;
                                dtpDetailDateEffective.Date = EffectiveDateValue;
                                dtpDetailDateEffective.Focus();
                            }
                        }
                    }

                    if (rowDataHasChanged)
                    {
                        FPetraUtilsObject.SetChangedFlag();
                    }

                    ((TFrmGLBatch)ParentForm).GetTransactionsControl().UpdateTransactionTotals(TGLBatchEnums.eGLLevel.Batch, UpdateTransactionDates);
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
        }

        private void ReverseBatch(System.Object sender, EventArgs e)
        {
            //get index position of row to post
            int newCurrentRowPos = GetSelectedRowIndex();

            if (FReverseLogicObject.ReverseBatch(FPreviouslySelectedDetailRow, dtpDetailDateEffective.Date.Value, FStartDateCurrentPeriod,
                    FEndDateLastForwardingPeriod))
            {
                // AlanP - commenting out most of this because it should be unnecessary - or should move to ShowDetailsManual()
                //Select unposted batch row in same index position as batch just posted
                //grdDetails.DataSource = null;
                //grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(FMainDS.ABatch.DefaultView);

                if (grdDetails.Rows.Count > 1)
                {
                    //Needed because posting process forces grid events which sets FDetailGridRowsCountPrevious = FDetailGridRowsCountCurrent
                    // such that a removal of a row is not detected
                    SelectRowInGrid(newCurrentRowPos);
                }
                else
                {
                    EnableButtonControl(false);
                    ClearDetailControls();
                    btnNew.Focus();
                    pnlDetails.Enabled = false;
                }

                UpdateRecordNumberDisplay();
                FFilterAndFindObject.SetRecordNumberDisplayProperties();
            }
        }

        private void PostBatch(System.Object sender, EventArgs e)
        {
            // Although the screen can be used with FINANCE-1, Posting requires FINANCE-2
            TSecurityChecks.CheckUserModulePermissions("FINANCE-2", "PostBatch [raised by Client Proxy for ModuleAccessManager]");

            if ((GetSelectedRowIndex() < 0) || (FPreviouslySelectedDetailRow == null))
            {
                MessageBox.Show(Catalog.GetString("Please select a GL Batch before posting!"));
                return;
            }

            //get index position of row to post
            int NewCurrentRowPos = GetSelectedRowIndex();

            TFrmGLBatch MainForm = (TFrmGLBatch) this.ParentForm;

            try
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.POSTING;

                if (FPostLogicObject.PostBatch(FPreviouslySelectedDetailRow, dtpDetailDateEffective.Date.Value, FStartDateCurrentPeriod,
                        FEndDateLastForwardingPeriod))
                {
                    // AlanP - commenting out most of this because it should be unnecessary - or should move to ShowDetailsManual()
                    ////Select unposted batch row in same index position as batch just posted
                    //grdDetails.DataSource = null;
                    //grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(FMainDS.ABatch.DefaultView);

                    if (grdDetails.Rows.Count > 1)
                    {
                        //Needed because posting process forces grid events which sets FDetailGridRowsCountPrevious = FDetailGridRowsCountCurrent
                        // such that a removal of a row is not detected
                        SelectRowInGrid(NewCurrentRowPos);
                    }
                    else
                    {
                        EnableButtonControl(false);
                        ClearDetailControls();
                        btnNew.Focus();
                        pnlDetails.Enabled = false;
                    }

                    UpdateRecordNumberDisplay();
                    FFilterAndFindObject.SetRecordNumberDisplayProperties();
                }
            }
            finally
            {
                MainForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.NONE;
            }
        }

        /// <summary>
        /// this function calculates the balances of the accounts involved, if this batch would be posted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestPostBatch(System.Object sender, EventArgs e)
        {
            if (FPreviouslySelectedDetailRow == null)
            {
                MessageBox.Show(Catalog.GetString("There is no current Batch row selected!"),
                    Catalog.GetString("Test Post Batch"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            TFrmGLBatch GLBatchForm = (TFrmGLBatch) this.ParentForm;

            try
            {
                GLBatchForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.TESTING;

                FPostLogicObject.TestPostBatch(FPreviouslySelectedDetailRow);
            }
            finally
            {
                GLBatchForm.FCurrentGLBatchAction = TGLBatchEnums.GLBatchAction.NONE;
            }
        }

        private void RefreshGridData(int ABatchNumber, bool ANoFocusChange, bool ASelectOnly = false)
        {
            //string RowFilter = string.Empty;

            if (!ASelectOnly)
            {
                //RowFilter = String.Format("({0}) AND ({1})", FPeriodFilter, FStatusFilter);

                //// AlanP: review this
                //FFilterAndFindObject.FilterPanelControls.SetBaseFilter(RowFilter, (FSelectedPeriod == -1)
                //    && (FCurrentBatchViewOption == MFinanceConstants.GL_BATCH_VIEW_ALL));
                FFilterAndFindObject.ApplyFilter();
            }

            if (grdDetails.Rows.Count < 2)
            {
                ShowDetails(null);
                ((TFrmGLBatch) this.ParentForm).DisableJournals();
                ((TFrmGLBatch) this.ParentForm).DisableTransactions();
            }
            else if (FBatchesLoaded == true)
            {
                //Select same row after refilter
                int newRowToSelectAfterFilter =
                    (ABatchNumber > 0) ? GetDataTableRowIndexByPrimaryKeys(FLedgerNumber, ABatchNumber) : FPrevRowChangedRow;

                if (ANoFocusChange)
                {
                    SelectRowInGrid(newRowToSelectAfterFilter);
                    //grdDetails.SelectRowWithoutFocus(newRowToSelectAfterFilter);
                }
                else
                {
                    SelectRowInGrid(newRowToSelectAfterFilter);
                }
            }
        }
    }
}