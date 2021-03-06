//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       wolfgangb
//
// Copyright 2004-2010 by OM International
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
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon.Validation;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MPersonnel.Personnel.Data;
using Ict.Petra.Shared.MPersonnel.Units.Data;
using Ict.Petra.Shared.MPartner.Validation;

namespace Ict.Petra.Shared.MPersonnel.Validation
{
    /// <summary>
    /// Contains functions for the validation of MPersonnel Personnel DataTables.
    /// </summary>
    public static partial class TSharedPersonnelValidation_Personnel
    {
        /// <summary>
        /// Validates the Commitment data of a Partner.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateCommitmentManual(object AContext, PmStaffDataRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Receiving Field' must be a Partner of Class 'UNIT' and must not be 0
            ValidationColumn = ARow.Table.Columns[PmStaffDataTable.ColumnReceivingFieldId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(
                    ARow.ReceivingField, false, THelper.NiceValueDescription(
                        ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                // ResultText!
                AVerificationResultCollection.Remove(ValidationColumn);
                AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
            }

            // 'Home Office' must be a Partner of Class 'UNIT'
            ValidationColumn = ARow.Table.Columns[PmStaffDataTable.ColumnHomeOfficeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(ARow.HomeOffice, false,
                    THelper.NiceValueDescription(ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                // ResultText!
                AVerificationResultCollection.Remove(ValidationColumn);
                AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
            }

            // 'Recruiting Office' must be a Partner of Class 'UNIT'
            ValidationColumn = ARow.Table.Columns[PmStaffDataTable.ColumnOfficeRecruitedById];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(ARow.OfficeRecruitedBy, false,
                    THelper.NiceValueDescription(ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                // ResultText!
                AVerificationResultCollection.Remove(ValidationColumn);
                AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
            }

            // 'End of Commitment' must be later than 'Start of Commitment'
            ValidationColumn = ARow.Table.Columns[PmStaffDataTable.ColumnEndOfCommitmentId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.EndOfCommitment, ARow.StartOfCommitment,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Status' must have a value
            ValidationColumn = ARow.Table.Columns[PmStaffDataTable.ColumnStatusCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.StatusCode,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the Job Assignment data of a Partner.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateJobAssignmentManual(object AContext, PmJobAssignmentRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'From Date' must be defined
            ValidationColumn = ARow.Table.Columns[PmJobAssignmentTable.ColumnFromDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedValidationControlHelper.IsNotInvalidDate(ARow.FromDate,
                    ValidationControlsData.ValidationControlLabel, AVerificationResultCollection, true,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'To Date' must be later than 'From Date', must not be null and must not be more than 2 years from now
            ValidationColumn = ARow.Table.Columns[PmJobAssignmentTable.ColumnToDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedValidationControlHelper.IsNotInvalidDate(ARow.ToDate,
                    ValidationControlsData.ValidationControlLabel, AVerificationResultCollection, true,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);

                if (VerificationResult == null)
                {
                    VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.ToDate, ARow.FromDate,
                        ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                    // Handle addition to/removal from TVerificationResultCollection
                    AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);

                    if (VerificationResult == null)
                    {
                        VerificationResult = TDateChecks.IsDateBetweenDates(ARow.ToDate, ARow.FromDate, DateTime.Today.AddYears(2),
                            ValidationControlsData.ValidationControlLabel,
                            TDateBetweenDatesCheckType.dbdctUnspecific, TDateBetweenDatesCheckType.dbdctUnspecific,
                            AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                        // Handle addition to/removal from TVerificationResultCollection
                        AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
                    }
                }
            }

            // 'Unit' must be a Partner of Class 'UNIT' and must not be 0
            ValidationColumn = ARow.Table.Columns[PmJobAssignmentTable.ColumnUnitKeyId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(
                    ARow.UnitKey, false, THelper.NiceValueDescription(
                        ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                // ResultText!
                AVerificationResultCollection.Remove(ValidationColumn);
                AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
            }

            // 'Assignment Type' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmJobAssignmentTable.ColumnAssignmentTypeCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtAssignmentTypeTable TypeTable;
                PtAssignmentTypeRow TypeRow;

                VerificationResult = null;

                if ((!ARow.IsAssignmentTypeCodeNull())
                    && (ARow.AssignmentTypeCode != String.Empty))
                {
                    TypeTable = (PtAssignmentTypeTable)TSharedDataCache.TMPersonnel.GetCacheableUnitsTable(
                        TCacheableUnitTablesEnum.JobAssignmentTypeList);
                    TypeRow = (PtAssignmentTypeRow)TypeTable.Rows.Find(ARow.AssignmentTypeCode);

                    // 'Assignment Type' must not be unassignable
                    if ((TypeRow != null)
                        && TypeRow.UnassignableFlag
                        && (TypeRow.IsUnassignableDateNull()
                            || (TypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Assignment Type' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmJobAssignmentTable.GetAssignmentTypeCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.AssignmentTypeCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Position' must be not be null and not unassignable
            ValidationColumn = ARow.Table.Columns[PmJobAssignmentTable.ColumnPositionNameId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtPositionTable PositionTable;
                PtPositionRow PositionRow;

                VerificationResult = null;

                if ((!ARow.IsPositionNameNull())
                    && (ARow.PositionName != String.Empty))
                {
                    PositionTable = (PtPositionTable)TSharedDataCache.TMPersonnel.GetCacheableUnitsTable(
                        TCacheableUnitTablesEnum.PositionList);
                    PositionRow = (PtPositionRow)PositionTable.Rows.Find(new object[] { ARow.PositionName, ARow.PositionScope });

                    // 'Position' must not be unassignable
                    if ((PositionRow != null)
                        && PositionRow.UnassignableFlag
                        && (PositionRow.IsUnassignableDateNull()
                            || (PositionRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Position' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmJobAssignmentTable.GetPositionNameDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.PositionName })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }
                else
                {
                    // Position name must not be null
                    VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.PositionName,
                        ValidationControlsData.ValidationControlLabel,
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the Passport data of a Partner.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidatePassportManual(object AContext, PmPassportDetailsRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Passport Number' must have a value
            ValidationColumn = ARow.Table.Columns[PmPassportDetailsTable.ColumnPassportNumberId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.PassportNumber,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Passport Name' must contain an opening and a closing paraenthesis
            ValidationColumn = ARow.Table.Columns[PmPassportDetailsTable.ColumnFullPassportNameId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                if ((!ARow.FullPassportName.Contains("("))
                    || (!ARow.FullPassportName.Contains(")")))
                {
                    VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                            ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_INDIV_DATA_PASSPORT_NAME_MISSING_PARAS,
                                new string[] { ValidationControlsData.ValidationControlLabel, ARow.FullPassportName })),
                        ValidationColumn, ValidationControlsData.ValidationControl);

                    // Handle addition to/removal from TVerificationResultCollection
                    AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
                }
            }

            // 'Expiry Date' must be later than 'Issue Date'
            ValidationColumn = ARow.Table.Columns[PmPassportDetailsTable.ColumnDateOfExpirationId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.DateOfExpiration, ARow.DateOfIssue,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Passport Type' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmPassportDetailsTable.ColumnPassportDetailsTypeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtPassportTypeTable TypeTable;
                PtPassportTypeRow TypeRow;

                VerificationResult = null;

                if ((!ARow.IsPassportDetailsTypeNull())
                    && (ARow.PassportDetailsType != String.Empty))
                {
                    TypeTable = (PtPassportTypeTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                        TCacheablePersonTablesEnum.PassportTypeList);
                    TypeRow = (PtPassportTypeRow)TypeTable.Rows.Find(ARow.PassportDetailsType);

                    // 'Passport Type' must not be unassignable
                    if ((TypeRow != null)
                        && TypeRow.UnassignableFlag
                        && (TypeRow.IsUnassignableDateNull()
                            || (TypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Passport Type' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmPassportDetailsTable.GetPassportDetailsTypeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.PassportDetailsType })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the personal document data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidatePersonalDocumentManual(object AContext, PmDocumentRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;
            PmDocumentTypeTable DocTypeTable;
            PmDocumentTypeRow DocTypeRow = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            ValidationColumn = ARow.Table.Columns[PmDocumentTable.ColumnDocCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                if ((!ARow.IsDocCodeNull())
                    && (ARow.DocCode != String.Empty))
                {
                    DocTypeTable = (PmDocumentTypeTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                        TCacheablePersonTablesEnum.DocumentTypeList);
                    DocTypeRow = (PmDocumentTypeRow)DocTypeTable.Rows.Find(ARow.DocCode);

                    // 'Document Type' must not be unassignable
                    if ((DocTypeRow != null)
                        && DocTypeRow.UnassignableFlag
                        && (DocTypeRow.IsUnassignableDateNull()
                            || (DocTypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Document Type' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmDocumentTable.GetDocCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.DocCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }
                else
                {
                    // 'Document Code' must have a value
                    VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.DocCode,
                        ValidationControlsData.ValidationControlLabel,
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Document Id' must have a value
            ValidationColumn = ARow.Table.Columns[PmDocumentTable.ColumnDocumentIdId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.DocumentId,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Issue Date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmDocumentTable.ColumnDateOfIssueId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.DateOfIssue, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Expiry Date' must be later or equal 'Start Date'
            ValidationColumn = ARow.Table.Columns[PmDocumentTable.ColumnDateOfExpirationId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.DateOfExpiration, ARow.DateOfStart,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the personal language data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidatePersonalLanguageManual(object AContext, PmPersonLanguageRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;
            PtLanguageLevelTable LanguageLevelTable;
            PtLanguageLevelRow LanguageLevelRow = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Language Level' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmPersonLanguageTable.ColumnLanguageLevelId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                if (!ARow.IsLanguageLevelNull())
                {
                    LanguageLevelTable = (PtLanguageLevelTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.LanguageLevelList);
                    LanguageLevelRow = (PtLanguageLevelRow)LanguageLevelTable.Rows.Find(ARow.LanguageLevel);

                    // 'Language Level' must not be unassignable
                    if ((LanguageLevelRow != null)
                        && LanguageLevelRow.UnassignableFlag
                        && (LanguageLevelRow.IsUnassignableDateNull()
                            || (LanguageLevelRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Language Level' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmPersonLanguageTable.GetLanguageLevelDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.LanguageLevel.ToString() })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the skill data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateSkillManual(object AContext, PmPersonSkillRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Skill Category' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmPersonSkillTable.ColumnSkillCategoryCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtSkillCategoryTable CategoryTable;
                PtSkillCategoryRow CategoryRow = null;

                VerificationResult = null;

                if (!ARow.IsSkillCategoryCodeNull())
                {
                    CategoryTable = (PtSkillCategoryTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.SkillCategoryList);
                    CategoryRow = (PtSkillCategoryRow)CategoryTable.Rows.Find(ARow.SkillCategoryCode);

                    // 'Skill Category' must not be unassignable
                    if ((CategoryRow != null)
                        && CategoryRow.UnassignableFlag
                        && (CategoryRow.IsUnassignableDateNull()
                            || (CategoryRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Skill Category' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmPersonSkillTable.GetSkillCategoryCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.SkillCategoryCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Skill Level' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmPersonSkillTable.ColumnSkillLevelId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtSkillLevelTable LevelTable;
                PtSkillLevelRow LevelRow = null;

                VerificationResult = null;

                if (!ARow.IsSkillLevelNull())
                {
                    LevelTable = (PtSkillLevelTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.SkillLevelList);
                    LevelRow = (PtSkillLevelRow)LevelTable.Rows.Find(ARow.SkillLevel);

                    // 'Skill Level' must not be unassignable
                    if ((LevelRow != null)
                        && LevelRow.UnassignableFlag
                        && (LevelRow.IsUnassignableDateNull()
                            || (LevelRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Skill Level' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmPersonSkillTable.GetSkillLevelDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.SkillLevel.ToString() })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }
                else
                {
                    // skill level must have a value
                    VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                            ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUE_NOT_ENTERED)),
                        ValidationColumn, ValidationControlsData.ValidationControl);
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the previous experience data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidatePreviousExperienceManual(object AContext, PmPastExperienceRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Start Date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmPastExperienceTable.ColumnStartDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.StartDate, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'End Date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmPastExperienceTable.ColumnEndDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.EndDate, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'End Date' must be later or equal 'Start Date'
            ValidationColumn = ARow.Table.Columns[PmPastExperienceTable.ColumnEndDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.EndDate, ARow.StartDate,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the progress report (evaluation) data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateProgressReportManual(object AContext, PmPersonEvaluationRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Evaluator' must have a value
            ValidationColumn = ARow.Table.Columns[PmPersonEvaluationTable.ColumnEvaluatorId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.Evaluator,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Evaluation Date' must have a value
            ValidationColumn = ARow.Table.Columns[PmPersonEvaluationTable.ColumnEvaluationDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TSharedValidationControlHelper.IsNotInvalidDate(ARow.EvaluationDate,
                    ValidationControlsData.ValidationControlLabel, AVerificationResultCollection, true,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Next Evaluation Date' must have a value if evaluation type is not set to "Leaving"
            ValidationColumn = ARow.Table.Columns[PmPersonEvaluationTable.ColumnNextEvaluationDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                if (ARow.EvaluationType != "Leaving")
                {
                    VerificationResult = TSharedValidationControlHelper.IsNotInvalidDate(ARow.NextEvaluationDate,
                        ValidationControlsData.ValidationControlLabel, AVerificationResultCollection, true,
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Next Evaluation Date' must be later than 'Evaluation Date'
            ValidationColumn = ARow.Table.Columns[PmPersonEvaluationTable.ColumnNextEvaluationDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.NextEvaluationDate, ARow.EvaluationDate,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Report Type' must have a value
            ValidationColumn = ARow.Table.Columns[PmPersonEvaluationTable.ColumnEvaluationTypeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.EvaluationType,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the personal (miscellaneous) data of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidatePersonalDataManual(object AContext, PmPersonalDataRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Believer since year' must have a sensible value (must not be below 1850 and must not lie in the future)
            ValidationColumn = ARow.Table.Columns[PmPersonalDataTable.ColumnBelieverSinceYearId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                if (!ARow.IsBelieverSinceYearNull() && (ARow.BelieverSinceYear != 0))
                {
                    VerificationResult = TDateChecks.IsDateBetweenDates(
                        new DateTime(ARow.BelieverSinceYear, 12, 31), new DateTime(1850, 1, 1), new DateTime(DateTime.Today.Year, 12, 31),
                        ValidationControlsData.ValidationControlLabel,
                        TDateBetweenDatesCheckType.dbdctUnrealisticDate, TDateBetweenDatesCheckType.dbdctNoFutureDate,
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                    // Handle addition to/removal from TVerificationResultCollection
                    AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
                }
            }
        }

        /// <summary>
        /// Validates the general application record of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AEventApplication">true if application for event, false if application for field.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateGeneralApplicationManual(object AContext, PmGeneralApplicationRow ARow, bool AEventApplication,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Application Type' must have a value and must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnAppTypeNameId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.AppTypeName,
                    ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);

                PtApplicationTypeTable AppTypeTable;
                PtApplicationTypeRow AppTypeRow = null;

                VerificationResult = null;

                if (!ARow.IsAppTypeNameNull())
                {
                    AppTypeTable = (PtApplicationTypeTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.ApplicationTypeList);
                    AppTypeRow = (PtApplicationTypeRow)AppTypeTable.Rows.Find(ARow.AppTypeName);

                    // 'Application Type' must not be unassignable
                    if ((AppTypeRow != null)
                        && AppTypeRow.UnassignableFlag
                        && (AppTypeRow.IsUnassignableDateNull()
                            || (AppTypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Application Type' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmGeneralApplicationTable.GetAppTypeNameDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.AppTypeName })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Application Status' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenApplicationStatusId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtApplicantStatusTable AppStatusTable;
                PtApplicantStatusRow AppStatusRow = null;

                VerificationResult = null;

                if (!ARow.IsGenApplicationStatusNull())
                {
                    AppStatusTable = (PtApplicantStatusTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.ApplicantStatusList);
                    AppStatusRow = (PtApplicantStatusRow)AppStatusTable.Rows.Find(ARow.GenApplicationStatus);

                    // 'Application Status' must not be unassignable
                    if ((AppStatusRow != null)
                        && AppStatusRow.UnassignableFlag
                        && (AppStatusRow.IsUnassignableDateNull()
                            || (AppStatusRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Application Status' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmGeneralApplicationTable.GetGenApplicationStatusDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.GenApplicationStatus })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // following validation only relevant for event applications
            if (AEventApplication)
            {
                // 'Organization Contact 1' must not be unassignable
                ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenContact1Id];

                if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
                {
                    PtContactTable ContactTable;
                    PtContactRow ContactRow;

                    VerificationResult = null;

                    if ((!ARow.IsGenContact1Null())
                        && (ARow.GenContact1 != String.Empty))
                    {
                        ContactTable = (PtContactTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                            TCacheablePersonTablesEnum.ContactList);
                        ContactRow = (PtContactRow)ContactTable.Rows.Find(ARow.GenContact1);

                        // 'Contact' must not be unassignable
                        if ((ContactRow != null)
                            && ContactRow.UnassignableFlag
                            && (ContactRow.IsUnassignableDateNull()
                                || (ContactRow.UnassignableDate <= DateTime.Today)))
                        {
                            // if 'Contact' is unassignable then check if the value has been changed or if it is a new record
                            if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmGeneralApplicationTable.GetGenContact1DBName()))
                            {
                                VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                        ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                            new string[] { ValidationControlsData.ValidationControlLabel, ARow.GenContact1 })),
                                    ValidationColumn, ValidationControlsData.ValidationControl);
                            }
                        }
                    }

                    // Handle addition/removal to/from TVerificationResultCollection
                    AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
                }

                // 'Organization Contact 2' must not be unassignable
                ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenContact2Id];

                if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
                {
                    PtContactTable ContactTable;
                    PtContactRow ContactRow;

                    VerificationResult = null;

                    if ((!ARow.IsGenContact2Null())
                        && (ARow.GenContact2 != String.Empty))
                    {
                        ContactTable = (PtContactTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                            TCacheablePersonTablesEnum.ContactList);
                        ContactRow = (PtContactRow)ContactTable.Rows.Find(ARow.GenContact2);

                        // 'Contact' must not be unassignable
                        if ((ContactRow != null)
                            && ContactRow.UnassignableFlag
                            && (ContactRow.IsUnassignableDateNull()
                                || (ContactRow.UnassignableDate <= DateTime.Today)))
                        {
                            // if 'Contact' is unassignable then check if the value has been changed or if it is a new record
                            if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmGeneralApplicationTable.GetGenContact2DBName()))
                            {
                                VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                        ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                            new string[] { ValidationControlsData.ValidationControlLabel, ARow.GenContact2 })),
                                    ValidationColumn, ValidationControlsData.ValidationControl);
                            }
                        }
                    }

                    // Handle addition/removal to/from TVerificationResultCollection
                    AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
                }
            }

            // following validation only relevant for field applications
            if (!AEventApplication)
            {
                // Field Application: 'Field' must be a Partner of Class 'UNIT' and must not be 0 and not be null
                ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenAppPossSrvUnitKeyId];

                if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
                {
                    if (ARow.IsGenAppPossSrvUnitKeyNull())
                    {
                        VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_PARTNERKEY_INVALID_NOTNULL,
                                    new string[] { ValidationControlsData.ValidationControlLabel })),
                            ValidationColumn, ValidationControlsData.ValidationControl);
                    }
                    else
                    {
                        VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(
                            ARow.GenAppPossSrvUnitKey, false, THelper.NiceValueDescription(
                                ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                            AContext, ValidationColumn, ValidationControlsData.ValidationControl);
                    }

                    // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                    // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                    // ResultText!
                    AVerificationResultCollection.Remove(ValidationColumn);
                    AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
                }
            }

            // 'Cancellation date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenAppCancelledId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.GenAppCancelled, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Accepted by sending field date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenAppSendFldAcceptDateId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.GenAppSendFldAcceptDate, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Accepted by receiving field date' must not be a future date
            ValidationColumn = ARow.Table.Columns[PmGeneralApplicationTable.ColumnGenAppRecvgFldAcceptId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = null;

                VerificationResult = TDateChecks.IsCurrentOrPastDate(ARow.GenAppRecvgFldAccept, ValidationControlsData.ValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }

        /// <summary>
        /// Validates the event (short term) application record of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateEventApplicationManual(object AContext, PmShortTermApplicationRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Event' must be a Partner of Class 'UNIT' and must not be 0
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnStConfirmedOptionId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                if (ARow.IsStConfirmedOptionNull())
                {
                    VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                            ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_PARTNERKEY_INVALID_NOTNULL,
                                new string[] { ValidationControlsData.ValidationControlLabel })),
                        ValidationColumn, ValidationControlsData.ValidationControl);
                }
                else
                {
                    VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(
                        ARow.StConfirmedOption, false, THelper.NiceValueDescription(
                            ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);
                }

                // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                // ResultText!
                AVerificationResultCollection.Remove(ValidationColumn);
                AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
            }

            // 'Charged Field' must be a Partner of Class 'UNIT'
            //
            // HOWEVER, 'null' is a perfectly valid value for 'Charged Field' (according to WolfgangB).
            // If it is null then we must not call TSharedPartnerValidation_Partner.IsValidUNITPartner
            // as the attempt to retrieve 'ARow.StFieldCharged' would result in
            // 'System.Data.StrongTypingException("Error: DB null", null)'!!!
            if (!ARow.IsStFieldChargedNull())
            {
                ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnStFieldChargedId];

                if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
                {
                    VerificationResult = TSharedPartnerValidation_Partner.IsValidUNITPartner(
                        ARow.StFieldCharged, true, THelper.NiceValueDescription(
                            ValidationControlsData.ValidationControlLabel) + " must be set correctly.",
                        AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                    // Since the validation can result in different ResultTexts we need to remove any validation result manually as a call to
                    // AVerificationResultCollection.AddOrRemove wouldn't remove a previous validation result with a different
                    // ResultText!
                    AVerificationResultCollection.Remove(ValidationColumn);
                    AVerificationResultCollection.AddAndIgnoreNullValue(VerificationResult);
                }
            }

            // 'Arrival Method' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnTravelTypeToCongCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtTravelTypeTable TravelTypeTable;
                PtTravelTypeRow TravelTypeRow = null;

                VerificationResult = null;

                if (!ARow.IsTravelTypeToCongCodeNull())
                {
                    TravelTypeTable = (PtTravelTypeTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.TransportTypeList);
                    TravelTypeRow = (PtTravelTypeRow)TravelTypeTable.Rows.Find(ARow.TravelTypeToCongCode);

                    // 'Arrival Method' must not be unassignable
                    if ((TravelTypeRow != null)
                        && TravelTypeRow.UnassignableFlag
                        && (TravelTypeRow.IsUnassignableDateNull()
                            || (TravelTypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Arrival Method' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmShortTermApplicationTable.GetTravelTypeToCongCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.TravelTypeToCongCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Departure Method' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnTravelTypeFromCongCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtTravelTypeTable TravelTypeTable;
                PtTravelTypeRow TravelTypeRow = null;

                VerificationResult = null;

                if (!ARow.IsTravelTypeFromCongCodeNull())
                {
                    TravelTypeTable = (PtTravelTypeTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTableDelegate(
                        TCacheablePersonTablesEnum.TransportTypeList);
                    TravelTypeRow = (PtTravelTypeRow)TravelTypeTable.Rows.Find(ARow.TravelTypeFromCongCode);

                    // 'Departure Method' must not be unassignable
                    if ((TravelTypeRow != null)
                        && TravelTypeRow.UnassignableFlag
                        && (TravelTypeRow.IsUnassignableDateNull()
                            || (TravelTypeRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Departure Method' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmShortTermApplicationTable.GetTravelTypeFromCongCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.TravelTypeFromCongCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Departure Date' must be later than 'Arrival Date'
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnDepartureId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.Departure, ARow.Arrival,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Arrival Hour' must be between 0 and 24
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnArrivalHourId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TNumericalChecks.IsInRange(ARow.ArrivalHour, 0, 24,
                    Catalog.GetString("Arrival Hour"),
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Arrival Minute' must be between 0 and 59
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnArrivalMinuteId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TNumericalChecks.IsInRange(ARow.ArrivalMinute, 0, 59,
                    Catalog.GetString("Arrival Minute"),
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Departure Hour' must be between 0 and 24
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnDepartureHourId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TNumericalChecks.IsInRange(ARow.DepartureHour, 0, 24,
                    Catalog.GetString("Departure Hour"),
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Departure Minute' must be between 0 and 59
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnDepartureMinuteId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TNumericalChecks.IsInRange(ARow.DepartureMinute, 0, 59,
                    Catalog.GetString("Departure Minute"),
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Arrival Point' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnArrivalPointCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtArrivalPointTable ArrivalPointTable;
                PtArrivalPointRow ArrivalPointRow;

                VerificationResult = null;

                if ((!ARow.IsArrivalPointCodeNull())
                    && (ARow.ArrivalPointCode != String.Empty))
                {
                    ArrivalPointTable = (PtArrivalPointTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                        TCacheablePersonTablesEnum.ArrivalDeparturePointList);
                    ArrivalPointRow = (PtArrivalPointRow)ArrivalPointTable.Rows.Find(ARow.ArrivalPointCode);

                    // 'Arrival Point' must not be unassignable
                    if ((ArrivalPointRow != null)
                        && ArrivalPointRow.UnassignableFlag
                        && (ArrivalPointRow.IsUnassignableDateNull()
                            || (ArrivalPointRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Contact' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmShortTermApplicationTable.GetArrivalPointCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.ArrivalPointCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            // 'Departure Point' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmShortTermApplicationTable.ColumnDeparturePointCodeId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                PtArrivalPointTable ArrivalPointTable;
                PtArrivalPointRow ArrivalPointRow;

                VerificationResult = null;

                if ((!ARow.IsDeparturePointCodeNull())
                    && (ARow.DeparturePointCode != String.Empty))
                {
                    ArrivalPointTable = (PtArrivalPointTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                        TCacheablePersonTablesEnum.ArrivalDeparturePointList);
                    ArrivalPointRow = (PtArrivalPointRow)ArrivalPointTable.Rows.Find(ARow.DeparturePointCode);

                    // 'Arrival Point' must not be unassignable
                    if ((ArrivalPointRow != null)
                        && ArrivalPointRow.UnassignableFlag
                        && (ArrivalPointRow.IsUnassignableDateNull()
                            || (ArrivalPointRow.UnassignableDate <= DateTime.Today)))
                    {
                        // if 'Arrival Point' is unassignable then check if the value has been changed or if it is a new record
                        if (TSharedValidationHelper.IsRowAddedOrFieldModified(ARow, PmShortTermApplicationTable.GetDeparturePointCodeDBName()))
                        {
                            VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                    ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                        new string[] { ValidationControlsData.ValidationControlLabel, ARow.DeparturePointCode })),
                                ValidationColumn, ValidationControlsData.ValidationControl);
                        }
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }

            //TODO: if arrival   hour == 24 then arrival   minute must be 0
            //TODO: if departure hour == 24 then departure minute must be 0

            //TODO: make sure that no other application already exists for this event and this person
        }

        /// <summary>
        /// Validates the field (long term) application record of a Person.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        /// <param name="AValidationControlsDict">A <see cref="TValidationControlsDict" /> containing the Controls that
        /// display data that is about to be validated.</param>
        /// <returns>void</returns>
        public static void ValidateFieldApplicationManual(object AContext, PmYearProgramApplicationRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection, TValidationControlsDict AValidationControlsDict)
        {
            DataColumn ValidationColumn;
            TValidationControlsData ValidationControlsData;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Available to' must be later than 'Available from' date
            ValidationColumn = ARow.Table.Columns[PmYearProgramApplicationTable.ColumnEndOfCommitmentId];

            if (AValidationControlsDict.TryGetValue(ValidationColumn, out ValidationControlsData))
            {
                VerificationResult = TDateChecks.FirstGreaterOrEqualThanSecondDate(ARow.EndOfCommitment, ARow.StartOfCommitment,
                    ValidationControlsData.ValidationControlLabel, ValidationControlsData.SecondValidationControlLabel,
                    AContext, ValidationColumn, ValidationControlsData.ValidationControl);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult, ValidationColumn);
            }
        }
    }
}