﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       alanp
//
// Copyright 2004-2015 by OM International
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ict.Tools.DevelopersAssistant
{
    /// <summary>
    /// Class for the dialog that requests a bug number to display in Mantis
    /// </summary>
    public partial class DlgBugNumber : Form
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public DlgBugNumber()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string bugAsString = txtBugNumber.Text;
            bool isOk = false;

            if (bugAsString != String.Empty)
            {
                Int32 bugAsInt = -1;

                if (Int32.TryParse(bugAsString, out bugAsInt))
                {
                    isOk = true;
                }
            }

            if (isOk)
            {
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a numeric value for the case number.", Program.APP_TITLE);
                this.DialogResult = System.Windows.Forms.DialogResult.None;
            }
        }
    }
}