//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2013 by OM International
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
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Resources;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.IO; // implicit reference
using Ict.Common.Printing;
using Ict.Common.Verification;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Client.MReporting.Gui;
using Ict.Petra.Client.MReporting.Logic;
using Ict.Petra.Shared.MReporting;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared.Interfaces.MPartner;


namespace Ict.Petra.Client.MReporting.Gui
{
    /// <summary>
    ///
    /// </summary>
    public partial class TFrmPrintPreview : System.Windows.Forms.Form, Ict.Petra.Client.CommonForms.IFrmPetra
    {
        #region private members

        private TGridPreview FGridPreview;
        private bool FPrinterInstalled;

        private TReportPrinterLayout ReportTxtPrinter;
        private TGfxPrinter FGfxPrinter;
        private TTxtPrinter FTxtPrinter;
        private TResultList Results;
        private TParameterList Parameters;
        private string ReportName;
        private TPrintChartCallbackProcedure PrintChartProcedure;
        private bool PrintChartProcedureValid;
        private TFrmPetraUtils FPetraUtilsObject;
        private TRptCalculator FCalculator;
        private bool FWrapColumn;

        #endregion

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ACallerForm"></param>
        /// <param name="caption">caption of the dialog</param>
        /// <param name="duration"></param>
        /// <param name="results"></param>
        /// <param name="parameters"></param>
        /// <param name="AWrapColumn">True: Wrap the text in the column. False: Cut the text that is too long for the column</param>
        /// <param name="ACalculator"></param>
        public TFrmPrintPreview(Form ACallerForm, String caption, TimeSpan duration, TResultList results,
            TParameterList parameters, bool AWrapColumn, TRptCalculator ACalculator)
            : base()
        {
            FPetraUtilsObject = new Ict.Petra.Client.CommonForms.TFrmPetraUtils(ACallerForm, this, stbMain);
            FCalculator = ACalculator;

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            #region CATALOGI18N

            // this code has been inserted by GenerateI18N, all changes in this region will be overwritten by GenerateI18N
            this.mniFile.Text = Catalog.GetString("&File");
            this.mniFileClose.Text = Catalog.GetString("&Close");
            this.mniFileClose.ToolTipText = Catalog.GetString("Close the preview");
            this.mniHelp.Text = Catalog.GetString("&Help");
            this.mniHelpPetraHelp.Text = Catalog.GetString("&Petra Help");
            this.mniHelpBugReport.Text = Catalog.GetString("Bug &Report");
            this.mniHelpAboutPetra.Text = Catalog.GetString("&About OpenPetra...");
            this.mniHelpDevelopmentTeam.Text = Catalog.GetString("&The Development Team...");
            this.stbMain.Text = Catalog.GetString("Status Bar");
            this.stpInfo.Text = Catalog.GetString("Ready");
            this.tbbClose.Text = Catalog.GetString("Close");
            this.tbbClose.ToolTipText = Catalog.GetString("Closes this window");
            this.Text = Catalog.GetString("Print Preview");
            this.tbpText.Text = Catalog.GetString("Text Preview");
            this.txtOutput.Text = Catalog.GetString("Text Output");
            this.tbpPreview.Text = Catalog.GetString("Print Preview");
            this.lblNoPrinter.Text = Catalog.GetString("Unfortunately this function is disabled. Please install a printer to use this page.");
            this.CbB_Zoom.Text = Catalog.GetString("Select Zoom");
            this.Btn_PreviousPage.Text = Catalog.GetString("Previous Page");
            this.Btn_NextPage.Text = Catalog.GetString("Next Page");
            this.tbpGridView.Text = Catalog.GetString("Detail Reports");
            this.tbtPrint.Text = Catalog.GetString("Print");
            this.tbtPrint.ToolTipText = Catalog.GetString("Print the report");
            this.tbtExportCSV.Text = Catalog.GetString("Export to CSV");
            this.tbtExportCSV.ToolTipText = Catalog.GetString("Export to CSV text file");
            this.tbtExportExcelFile.Text = Catalog.GetString("Export to Excel");
            this.tbtExportExcelFile.ToolTipText = Catalog.GetString("Export to Excel xlsx file or directly into Excel, if it is available");
            this.tbtExportText.Text = Catalog.GetString("Save as Text file");
            this.tbtExportText.ToolTipText = Catalog.GetString("Save as a text file (e.g. for email)");
            this.tbtSendEmail.Text = Catalog.GetString("Send Email");
            this.tbtSendEmail.ToolTipText = Catalog.GetString("Send the Report as an Email with Excel attachment");
            this.tbtCreateExtract.Text = Catalog.GetString("Create Extract");
            this.tbtCreateExtract.ToolTipText = Catalog.GetString("Create an Extract from this result set");
            this.tbtGenerateChart.Text = Catalog.GetString("Generate Chart");
            this.tbtGenerateChart.ToolTipText = Catalog.GetString(
                "Generates a chart in Excel (not available for all reports)");
            this.Text = Catalog.GetString("Print Preview");
            #endregion

            System.Windows.Forms.TabPage SelectedTab;

            this.Text = this.Text + ": " + caption;
            this.ReportName = caption;
            this.Results = results;
            this.Parameters = parameters;
            FTxtPrinter = new TTxtPrinter();
            this.ReportTxtPrinter = new TReportPrinterLayout(Results, Parameters, FTxtPrinter, AWrapColumn);
            ReportTxtPrinter.PrintReport();

            this.txtOutput.Lines = FTxtPrinter.GetArrayOfString();
            FPrinterInstalled = this.PrintDocument.PrinterSettings.IsValid;

            FWrapColumn = AWrapColumn;

            if (FPrinterInstalled)
            {
                this.tabPreview.SelectedTab = tbpPreview;
                FGfxPrinter = new TGfxPrinter(this.PrintDocument, TGfxPrinter.ePrinterBehaviour.eReport);

                // TReportPrinterLayout ReportGfxPrinter
                new TReportPrinterLayout(Results, Parameters, FGfxPrinter, AWrapColumn);

                this.PrintPreviewControl.Document = FGfxPrinter.Document;
                this.PrintPreviewControl.Zoom = 1; // show 100% by default
                this.PrintPreviewControl.UseAntiAlias = true;
                this.lblNoPrinter.Visible = false;
            }
            else
            {
                // PrintPreviewControl.CalculatePageInfo will throw InvalidPrinterException
                this.tabPreview.SelectedTab = tbpText;
                this.PrintPreviewControl.Visible = false;
                this.CbB_Zoom.Enabled = false;
                this.Btn_PreviousPage.Enabled = false;
                this.Btn_NextPage.Enabled = false;
            }

            this.PrintChartProcedure = null;
            this.PrintChartProcedureValid = false;
            sgGridView.SortableHeaders = false;
            sgGridView.SelectionMode = SourceGrid.GridSelectionMode.Cell;
            this.sgGridView.DoubleClickCell += new TDoubleClickCellEventHandler(this.SgGridView_DoubleClickCell);
            FGridPreview = new TGridPreview(this, FPetraUtilsObject, @PreviewDetailReport, Results, Parameters);

            if (!FGridPreview.PopulateResultGrid(sgGridView))
            {
                SelectedTab = tabPreview.SelectedTab;
                this.tabPreview.TabPages.Clear();
                this.tabPreview.TabPages.AddRange(new TabPage[] { this.tbpText, this.tbpPreview });
                tabPreview.SelectedTab = SelectedTab;
            }
            else
            {
                FGridPreview.PopulateGridContextMenu(ContextMenu1);
            }

            stpInfo.Text = string.Empty;
            stbMain.ShowMessage(String.Format(Catalog.GetString("It took {0} to calculate the report"), FormatDuration(duration)));
        }

        #region Event Handlers

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MniFile_Click(System.Object sender, System.EventArgs e)
        {
            if (sender == mniFileClose)
            {
                FPetraUtilsObject.ExecuteAction(eActionId.eClose);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MniHelpPetra_Click(System.Object sender, System.EventArgs e)
        {
            FPetraUtilsObject.ExecuteAction(eActionId.eHelp);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MniHelpAboutPetra_Click(System.Object sender, System.EventArgs e)
        {
            FPetraUtilsObject.ExecuteAction(eActionId.eHelpAbout);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MniHelpDevelopmentTeam_Click(System.Object sender, System.EventArgs e)
        {
            FPetraUtilsObject.ExecuteAction(eActionId.eHelpDevelopmentTeam);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MniHelpBugReport_Click(System.Object sender, System.EventArgs e)
        {
            FPetraUtilsObject.ExecuteAction(eActionId.eBugReport);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Btn_NextPage_Click(System.Object sender, System.EventArgs e)
        {
            this.PrintPreviewControl.StartPage = this.PrintPreviewControl.StartPage + 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Btn_PreviousPage_Click(System.Object sender, System.EventArgs e)
        {
            if (this.PrintPreviewControl.StartPage > 0)
            {
                this.PrintPreviewControl.StartPage = this.PrintPreviewControl.StartPage - 1;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void TPrintPreview_Resize(System.Object sender, System.EventArgs e)
        {
            /*System.Int32 origPage;
             * // somehow we need to get the scrollbars activated
             * // seems not to be needed anymore in .net 2.0; the workaround now causes an error
             * origPage = this.PrintPreviewControl.StartPage;
             * this.PrintPreviewControl.StartPage = this.PrintPreviewControl.StartPage - 1;
             * this.PrintPreviewControl.StartPage = origPage;*/
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbbCloseClick(object sender, EventArgs e)
        {
            FPetraUtilsObject.ExecuteAction(eActionId.eClose);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbtExportTextClick(System.Object sender, System.EventArgs e)
        {
            if (dlgSaveTextFile.FileName.Length == 0)
            {
                dlgSaveTextFile.FileName = this.ReportName + '.' + dlgSaveTextFile.DefaultExt;
            }

            if (dlgSaveTextFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FTxtPrinter.WriteToFile(dlgSaveTextFile.FileName);
            }
        }

        /// <summary>
        /// open the file directly in Excel, or save as Excel file
        /// </summary>
        protected void tbtExportExcelFileClick(System.Object sender, System.EventArgs e)
        {
            if (!OpenInExcel())
            {
                ExportToExcelFile();
            }
        }

        private String SelectColumnNameForExract(XmlNode node)
        {
            String Res = "";

            TFrmSelectExtractColumn SelectForm = new TFrmSelectExtractColumn();
            Boolean FoundInt64Field = false;

            foreach (XmlAttribute Attr in node.Attributes)
            {
                Int64 NumericField;

                if (Int64.TryParse(Attr.Value, out NumericField))
                {
                    FoundInt64Field = true;
                    SelectForm.AddOption(Attr.Name);
                }
            }

            if (FoundInt64Field && (SelectForm.ShowDialog() == System.Windows.Forms.DialogResult.OK))
            {
                Res = SelectForm.SelectedOption;
            }

            return Res;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbtCreateExtractClick(System.Object sender, System.EventArgs e)
        {
            XmlDocument doc = Results.WriteXmlDocument(Parameters, true);
            DataTable Tbl = new DataTable();

            Tbl.Columns.Add("PartnerKey", typeof(Int64));
            XmlNodeList Rows = doc.SelectNodes("*/Element");
            XmlNode FirstRow = Rows[0];
            String ColumnName = SelectColumnNameForExract(FirstRow);
            this.UseWaitCursor = true;

            Int64 repeatedPartnerKey = 0;

            foreach (XmlNode node in Rows)
            {
                XmlAttribute Attr = node.Attributes[ColumnName];

                if (Attr != null)
                {
                    Int64 intPartnerKey;

                    if (Int64.TryParse(Attr.Value, out intPartnerKey)
                        && (intPartnerKey != repeatedPartnerKey))
                    {
                        DataRow Row = Tbl.NewRow();
                        Row["PartnerKey"] = intPartnerKey;
                        Tbl.Rows.Add(Row);
                        repeatedPartnerKey = intPartnerKey;
                    }
                }
            }

            this.UseWaitCursor = false;

            if (Tbl.Rows.Count < 1)
            {
                MessageBox.Show(Catalog.GetString("Error - no Partner keys found"), Catalog.GetString("Generate Extract"));
                return;
            }

            TFrmExtractNamingDialog ExtractNameDialog = new TFrmExtractNamingDialog(this);
            string ExtractName;
            string ExtractDescription;

            ExtractNameDialog.ShowDialog();

            if (ExtractNameDialog.DialogResult != System.Windows.Forms.DialogResult.Cancel)
            {
                /* Get values from the Dialog */
                ExtractNameDialog.GetReturnedParameters(out ExtractName, out ExtractDescription);
            }
            else
            {
                // dialog was cancelled, do not continue with extract generation
                return;
            }

            ExtractNameDialog.Dispose();

            this.UseWaitCursor = true;

            // Create extract with given name and description and store it
            int ExtractId = 0;
            IPartnerUIConnectorsPartnerNewExtract PartnerExtractObject = TRemote.MPartner.Extracts.UIConnectors.PartnerNewExtract();
            Boolean CreateOk = PartnerExtractObject.CreateExtractFromListOfPartnerKeys(ExtractName, ExtractDescription, out ExtractId, Tbl, 0, false);
            this.UseWaitCursor = false;

            if (CreateOk)
            {
                MessageBox.Show(String.Format(Catalog.GetString("Extract Created with {0} Partners."),
                        Tbl.Rows.Count),
                    Catalog.GetString("Generate Extract"));
            }
            else
            {
                MessageBox.Show(Catalog.GetString("Creation of extract failed"),
                    Catalog.GetString("Generate Extract"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
        }

        /// <summary>
        /// export to csv file, and open it
        /// </summary>
        protected void tbtExportCSVClick(System.Object sender, System.EventArgs e)
        {
            ExportToCSV();
        }

        /// <summary>
        /// send email with Excel Attachment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbtSendEmailClick(System.Object sender, System.EventArgs e)
        {
            TFrmSendEmailOptionsDialog options = new TFrmSendEmailOptionsDialog(this);

            if (options.ShowDialog() == DialogResult.OK)
            {
                if (!options.AttachExcelFile && !options.AttachCSVFile && !options.AttachPDF)
                {
                    MessageBox.Show(Catalog.GetString("No Email has been sent because there are no attachments"),
                        Catalog.GetString("No Email has been sent"));
                    return;
                }

                TVerificationResultCollection verification;

                if (FCalculator.SendEmail(options.EmailAddresses, options.AttachExcelFile, options.AttachCSVFile, options.AttachPDF, FWrapColumn,
                        out verification))
                {
                    MessageBox.Show(Catalog.GetString("Email has been sent successfully"),
                        Catalog.GetString("Success"));
                }
                else
                {
                    MessageBox.Show(
                        verification.BuildVerificationResultString(),
                        Catalog.GetString("Email was not sent."));
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbtGenerateChartClick(System.Object sender, System.EventArgs e)
        {
            if (PrintChartProcedureValid)
            {
                PrintChartProcedure();
            }
            else
            {
                MessageBox.Show("There are no charts available for this report!");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void tbtPrintClick(System.Object sender, System.EventArgs e)
        {
            PrintDialog printerSelectionDlg;

            if (!FPrinterInstalled)
            {
                MessageBox.Show("The program cannot find a printer, and therefore cannot print!", "Problem with printing");
                return;
            }

            printerSelectionDlg = new PrintDialog();
            printerSelectionDlg.Document = FGfxPrinter.Document;

            printerSelectionDlg.PrinterSettings.MinimumPage = 1;
            printerSelectionDlg.PrinterSettings.MaximumPage = FGfxPrinter.NumberOfPages;
            printerSelectionDlg.PrinterSettings.FromPage = 1;
            printerSelectionDlg.PrinterSettings.ToPage = FGfxPrinter.NumberOfPages;

            printerSelectionDlg.AllowSomePages = true;
            printerSelectionDlg.AllowPrintToFile = true;
            printerSelectionDlg.AllowSelection = true;

            if (printerSelectionDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    FGfxPrinter.Document.Print();
                }
                catch (Exception E)
                {
                    TLogging.Log(E.StackTrace);
                    TLogging.Log(E.Message);
                    MessageBox.Show(E.Message, Catalog.GetString("Failed to save file"), MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    System.Console.WriteLine(E.StackTrace);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ACalculator"></param>
        protected void PreviewDetailReport(TRptCalculator ACalculator)
        {
            TFrmPrintPreview printWindow;

            // show a print window with all kinds of output options
            printWindow = new TFrmPrintPreview(this, ACalculator.GetParameters().Get("currentReport").ToString(),
                ACalculator.GetDuration(), ACalculator.GetResults(
                    ), ACalculator.GetParameters(), FWrapColumn, ACalculator);
            this.AddOwnedForm(printWindow);
            printWindow.Owner = this;

            // printWindow.SetPrintChartProcedure(GenerateChart);
            printWindow.ShowDialog();

            // EnableDisableToolbar(true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="printChartProcedure"></param>
        public void SetPrintChartProcedure(TPrintChartCallbackProcedure printChartProcedure)
        {
            this.PrintChartProcedure = printChartProcedure;
            this.PrintChartProcedureValid = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        protected void SgGridView_DoubleClickCell(System.Object Sender, SourceGrid.CellContextEventArgs e)
        {
            // double click on grid row activates the first menu item from the context menu
            if (ContextMenu1.MenuItems.Count > 0)
            {
                FGridPreview.MenuItemClick(ContextMenu1.MenuItems[0], null);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void PrintDocument_EndPrint(System.Object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            EnablePageButtons();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void CbB_Zoom_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            if (CbB_Zoom.SelectedItem.ToString() == "100%")
            {
                this.PrintPreviewControl.AutoZoom = false;
                this.PrintPreviewControl.Zoom = 1.0;
            }
            else if (CbB_Zoom.SelectedItem.ToString() == "75%")
            {
                this.PrintPreviewControl.AutoZoom = false;
                this.PrintPreviewControl.Zoom = 0.75;
            }
            else if (CbB_Zoom.SelectedItem.ToString() == "50%")
            {
                this.PrintPreviewControl.AutoZoom = false;
                this.PrintPreviewControl.Zoom = 0.5;
            }
            else
            {
                this.PrintPreviewControl.AutoZoom = true;
            }

            EnablePageButtons();
        }

        #endregion

        /// <summary>
        /// should this go into StringHelper?
        /// </summary>
        /// <returns>void</returns>
        private String FormatDuration(TimeSpan ADuration)
        {
            String ReturnValue;

            ReturnValue = "";

            if (ADuration.Hours >= 1)
            {
                ReturnValue = ReturnValue + String.Format("{0} hour", ADuration.Hours);

                if (ADuration.Hours != 1)
                {
                    ReturnValue = ReturnValue + 's';
                }

                ReturnValue = ReturnValue + ", ";
            }

            if ((ADuration.Minutes >= 1) || (ADuration.Hours > 0))
            {
                ReturnValue = ReturnValue + String.Format("{0} minute", ADuration.Minutes);

                if (ADuration.Minutes != 1)
                {
                    ReturnValue = ReturnValue + 's';
                }

                ReturnValue = ReturnValue + " and ";
            }

            ReturnValue = ReturnValue + String.Format("{0} second", ADuration.Seconds);

            if (ADuration.Minutes != 1)
            {
                ReturnValue = ReturnValue + 's';
            }

            return ReturnValue;
        }

        /// <summary>
        /// directly export the full result to Excel
        /// </summary>
        /// <returns>false if Excel is not available
        /// </returns>
        public bool OpenInExcel()
        {
            bool ReturnValue;
            TReportExcel myExcel;

            try
            {
                myExcel = new TReportExcel(Results, Parameters);
            }
            catch (Exception)
            {
                return false;
            }
            ReturnValue = true;
            myExcel.Show();
            myExcel.NewSheet("data");
            myExcel.ExportResult();
            myExcel.GiveUserControl();
            return ReturnValue;
        }

        /// <summary>
        /// export the full result to a CSV file
        ///
        /// </summary>
        /// <returns>void</returns>
        public void ExportToCSV()
        {
            System.Diagnostics.Process csvProcess;

            if (dlgSaveCSVFile.FileName.Length == 0)
            {
                dlgSaveCSVFile.FileName = this.ReportName + '.' + dlgSaveCSVFile.DefaultExt;
            }

            if (dlgSaveCSVFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool ExportOnlyLowestLevel = false;

                // Add the parameter export_only_lowest_level to the Parameters if you don't want to export the
                // higher levels. In some reports (Supporting Churches Report or Partner Contact Report) the csv
                // output looks much nicer if it doesn't contain the unnecessary higher levels.
                if (Parameters.Exists("csv_export_only_lowest_level"))
                {
                    ExportOnlyLowestLevel = Parameters.Get("csv_export_only_lowest_level").ToBool();
                }

                if (Results.WriteCSV(Parameters, dlgSaveCSVFile.FileName, ExportOnlyLowestLevel))
                {
                    try
                    {
                        csvProcess = new System.Diagnostics.Process();
                        csvProcess.EnableRaisingEvents = false;
                        csvProcess.StartInfo.FileName = dlgSaveCSVFile.FileName;
                        csvProcess.Start();
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    MessageBox.Show(Results.ErrorStatus, Catalog.GetString("Failed to save file"), MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        /// <summary>
        /// export to an Excel xlsx file
        /// </summary>
        public void ExportToExcelFile()
        {
            if (dlgSaveXLSXFile.FileName.Length == 0)
            {
                dlgSaveXLSXFile.FileName = this.ReportName + '.' + dlgSaveXLSXFile.DefaultExt;
            }

            if (dlgSaveXLSXFile.ShowDialog() == DialogResult.OK)
            {
                bool ExportOnlyLowestLevel = false;

                // Add the parameter export_only_lowest_level to the Parameters if you don't want to export the
                // higher levels. In some reports (Supporting Churches Report or Partner Contact Report) the csv
                // output looks much nicer if it doesn't contain the unnecessary higher levels.
                if (Parameters.Exists("csv_export_only_lowest_level"))
                {
                    ExportOnlyLowestLevel = Parameters.Get("csv_export_only_lowest_level").ToBool();
                }

                XmlDocument doc = Results.WriteXmlDocument(Parameters, ExportOnlyLowestLevel);

                if (doc != null)
                {
                    using (FileStream fs = new FileStream(dlgSaveXLSXFile.FileName, FileMode.Create))
                    {
                        if (TCsv2Xml.Xml2ExcelStream(doc, fs, false))
                        {
                            fs.Close();
                        }
                    }

                    try
                    {
                        System.Diagnostics.Process excelProcess;
                        excelProcess = new System.Diagnostics.Process();
                        excelProcess.EnableRaisingEvents = false;
                        excelProcess.StartInfo.FileName = dlgSaveXLSXFile.FileName;
                        excelProcess.Start();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, Catalog.GetString("Failed to save file"), MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                }
            }
        }

        /// <summary>
        /// Depending on the setting of zoom, several pages are displayed, or just one 100% or AutoZoom, will display only one page, and enable the previous/next page buttons otherwise display all pages, and scroll through
        /// </summary>
        /// <returns>void</returns>
        private void EnablePageButtons()
        {
            if ((CbB_Zoom.SelectedIndex == -1) || (CbB_Zoom.SelectedItem.ToString() == "Fit to Window")
                || (CbB_Zoom.SelectedItem.ToString() == "100%"))
            {
                Btn_NextPage.Enabled = FGfxPrinter.NumberOfPages > 1;
                Btn_PreviousPage.Enabled = FGfxPrinter.NumberOfPages > 1;
                this.PrintPreviewControl.Rows = 1;
            }
            else
            {
                Btn_NextPage.Enabled = false;
                Btn_PreviousPage.Enabled = false;
                this.PrintPreviewControl.Rows = FGfxPrinter.NumberOfPages;
            }
        }

        private void TxtOutputKeyPress(Object ASender, KeyPressEventArgs e)
        {
            // catch Ctrl A and select all text in the text box
            if ((System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                && (e.KeyChar == 1))
            {
                txtOutput.SelectAll();
                e.Handled = true;
            }
        }

        #region interface implementation

        /// <summary>
        /// This function tells the caller whether the window can be closed.
        /// It can be used to find out if something is still edited, for example.
        /// </summary>
        /// <returns>true if window can be closed
        /// </returns>
        public bool CanClose()
        {
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        public void HookupAllControls()
        {
        }

        /// <summary>
        /// needed for interface
        /// </summary>
        public TFrmPetraUtils GetPetraUtilsObject()
        {
            return FPetraUtilsObject;
        }

        /// <summary>
        /// todoComment
        /// </summary>
        public void RunOnceOnActivation()
        {
        }

        #endregion
    }

    /// <summary> </summary>
    public delegate void TPrintChartCallbackProcedure();
}
