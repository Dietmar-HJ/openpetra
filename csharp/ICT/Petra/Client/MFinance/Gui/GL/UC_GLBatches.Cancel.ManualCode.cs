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
using System.Data;
using System.Windows.Forms;

using Ict.Common;

using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Client.MFinance.Logic;

using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.GL.Data;

namespace Ict.Petra.Client.MFinance.Gui.GL
{
    /// <summary>
    /// A business logic class that handles cancelling of batches
    /// </summary>
    public class TUC_GLBatches_Cancel
    {
        private TFrmPetraEditUtils FPetraUtilsObject = null;
        private Int32 FLedgerNumber = 0;
        private GLBatchTDS FMainDS = null;
        private TFrmGLBatch FMyForm = null;

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public TUC_GLBatches_Cancel(TFrmPetraEditUtils APetraUtilsObject, Int32 ALedgerNumber, GLBatchTDS AMainDS)
        {
            FPetraUtilsObject = APetraUtilsObject;
            FLedgerNumber = ALedgerNumber;
            FMainDS = AMainDS;

            FMyForm = (TFrmGLBatch)FPetraUtilsObject.GetForm();
        }

        #endregion

        #region Main Public methods

        /// <summary>
        /// Method to cancel a specified batch
        /// </summary>
        /// <param name="ABatchRowToCancel">The row to cancel</param>
        /// <returns></returns>
        public bool CancelBatch(ABatchRow ABatchRowToCancel)
        {
            bool CancellationSuccessful = false;

            if ((ABatchRowToCancel == null) || (ABatchRowToCancel.BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return false;
            }

            int CurrentBatchNo = ABatchRowToCancel.BatchNumber;
            bool CurrentBatchJournalsLoadedAndCurrent = false;
            bool CurrentBatchTransactionsLoadedAndCurrent = false;

            string CancelMessage = string.Empty;
            string CompletionMessage = string.Empty;

            CancelMessage = String.Format(Catalog.GetString("Are you sure you want to cancel GL Batch {0}?"),
                CurrentBatchNo);

            if ((MessageBox.Show(CancelMessage,
                     "Cancel GL Batch",
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question,
                     MessageBoxDefaultButton.Button2) != System.Windows.Forms.DialogResult.Yes))
            {
                return false;
            }

            //Backup the Dataset for reversion purposes
            GLBatchTDS BackupMainDS = null;

            try
            {
                FMyForm.Cursor = Cursors.WaitCursor;

                //Backup the Dataset for reversion purposes
                BackupMainDS = (GLBatchTDS)FMainDS.GetChangesTyped(false);

                //Don't run an inactive fields check on this batch
                FMyForm.GetBatchControl().UpdateUnpostedBatchDictionary(CurrentBatchNo);

                //Check if current batch details are currently loaded
                CurrentBatchJournalsLoadedAndCurrent = (FMyForm.GetJournalsControl().FBatchNumber == CurrentBatchNo);
                CurrentBatchTransactionsLoadedAndCurrent = (FMyForm.GetTransactionsControl().FBatchNumber == CurrentBatchNo);

                //Save and check for inactive values in other unsaved Batches
                FPetraUtilsObject.SetChangedFlag();

                if (!FMyForm.SaveChangesManual(FMyForm.FCurrentGLBatchAction, !CurrentBatchJournalsLoadedAndCurrent,
                        !CurrentBatchTransactionsLoadedAndCurrent))
                {
                    FMyForm.GetBatchControl().UpdateUnpostedBatchDictionary();

                    CompletionMessage = String.Format(Catalog.GetString("GL Batch {0} has not been cancelled."),
                        CurrentBatchNo);

                    MessageBox.Show(CompletionMessage,
                        Catalog.GetString("GL Batch Cancellation"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return false;
                }

                //Remove any changes to current batch that may cause validation issues
                FMyForm.GetBatchControl().PrepareBatchDataForCancelling(CurrentBatchNo, true);

                //Clear the journal and trans tabs if current batch data is displayed there
                if (CurrentBatchJournalsLoadedAndCurrent)
                {
                    //Clear any transactions currently being edited in the Transaction Tab
                    FMyForm.GetJournalsControl().ClearCurrentSelection(CurrentBatchNo);
                }

                if (CurrentBatchTransactionsLoadedAndCurrent)
                {
                    //Clear any transactions currently being edited in the Transaction Tab
                    FMyForm.GetTransactionsControl().ClearCurrentSelection(CurrentBatchNo);
                }

                //Delete transactions and attributes
                FMyForm.GetTransactionsControl().DeleteTransactionData(CurrentBatchNo);

                //Journals are not deleted. Update Journal totals and status
                foreach (AJournalRow journal in FMainDS.AJournal.Rows)
                {
                    if (journal.BatchNumber == CurrentBatchNo)
                    {
                        journal.BeginEdit();
                        journal.JournalCreditTotal = 0;
                        journal.JournalDebitTotal = 0;
                        journal.LastTransactionNumber = 0;
                        journal.JournalStatus = MFinanceConstants.BATCH_CANCELLED;
                        journal.EndEdit();
                    }
                }

                // Edit the batch row
                ABatchRowToCancel.BeginEdit();
                ABatchRowToCancel.BatchCreditTotal = 0;
                ABatchRowToCancel.BatchDebitTotal = 0;
                ABatchRowToCancel.BatchControlTotal = 0;
                ABatchRowToCancel.BatchStatus = MFinanceConstants.BATCH_CANCELLED;
                ABatchRowToCancel.EndEdit();

                FPetraUtilsObject.SetChangedFlag();
                FMyForm.SaveChangesManual();

                CompletionMessage = String.Format(Catalog.GetString("Batch no.: {0} cancelled successfully."),
                    CurrentBatchNo);

                MessageBox.Show(CompletionMessage,
                    Catalog.GetString("Batch Cancelled"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                FMyForm.DisableTransactions();
                FMyForm.DisableJournals();

                CancellationSuccessful = true;
            }
            catch (Exception ex)
            {
                //Revert to previous state
                if (BackupMainDS != null)
                {
                    FMainDS.RejectChanges();
                    FMainDS.Merge(BackupMainDS);

                    FMyForm.GetBatchControl().ShowDetailsRefresh();
                }

                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
            finally
            {
                FMyForm.Cursor = Cursors.Default;
            }

            return CancellationSuccessful;
        }

        #endregion
    }
}