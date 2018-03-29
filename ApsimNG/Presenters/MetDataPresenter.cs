﻿// -----------------------------------------------------------------------
// <copyright file="MetDataPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.IO;
    using System.Text;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Graph;
    using Views;

    /// <summary>A presenter for displaying weather data</summary>
    public class MetDataPresenter : IPresenter
    {
        /// <summary>The met data</summary>
        private Weather weatherData;

        /// <summary>The met data view</summary>
        private IMetDataView weatherDataView;

        // these are used to display the graphs, and refresh graphs as required

        /// <summary>Hold the data used by the graphs</summary>
        private DataTable graphMetData;

        /// <summary>
        /// The list of sheet names
        /// </summary>
        private List<string> sheetNames;

        /// <summary>Hold the first date in datatable, for use in the graphs</summary>
        private DateTime dataFirstDate;

        /// <summary>Hold the last date in datatable, for use in the graphs</summary>
        private DateTime dataLastDate;

        /// <summary>Hold the first date in datatable; may include partial years</summary>
        private DateTime dataStartDate;

        /// <summary>Hold the last date in datatable; may include partial years</summary>
        private DateTime dataEndDate;

        /// <summary>Hold an array of months for the graph,  by default, is set to will Jan yyyy to Dec yyyy, except where
        /// data being displays is not for full year</summary>
        private string[] monthsToDisplay = DateUtilities.LowerCaseMonths;

        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Attaches the specified model.</summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <param name="explorerPresenter">The explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            this.weatherData = model as Weather;
            this.weatherDataView = view as IMetDataView;

            this.weatherDataView.BrowseClicked += this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked += this.GraphRefreshValueChanged;
            this.weatherDataView.ExcelSheetChangeClicked += this.ExcelSheetValueChanged;

            this.WriteTableAndSummary(this.weatherData.FullFileName, this.weatherData.ExcelWorkSheetName);
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.weatherDataView.BrowseClicked -= this.OnBrowse;
            this.weatherDataView.GraphRefreshClicked -= this.GraphRefreshValueChanged;
            this.weatherDataView.ExcelSheetChangeClicked -= this.ExcelSheetValueChanged;
        }

        /// <summary>Called when [browse].</summary>
        /// <param name="fileName">Name of the file.</param>
        public void OnBrowse(string fileName)
        {
            if (this.weatherData.FullFileName != PathUtilities.GetAbsolutePath(fileName, this.explorerPresenter.ApsimXFile.FileName))
            {
                if (Path.GetExtension(fileName) == ExcelUtilities.ExcelExtension)
                {
                    //// Extend height of Browse Panel to show Drop Down for Sheet names
                    this.weatherDataView.ShowExcelSheets(true);
                    this.sheetNames = ExcelUtilities.GetWorkSheetNames(fileName);
                    this.weatherDataView.PopulateDropDownData(this.sheetNames);

                    // the following is not required here as it happens when the sheet name is changed
                    // this.WriteTableAndSummary(fileName);
                }
                else
                {
                    //// Shrink Browse Panel so that the sheet name dropdown doesn't show
                    this.weatherDataView.ShowExcelSheets(false);

                    // as a precaution, set this to nothing
                    this.weatherData.ExcelWorkSheetName = string.Empty;
                    this.WriteTableAndSummary(fileName);
                }
            }
        }

        /// <summary>
        /// This is called when the Graph StartYear or the Graphing ShowYears Numeric updown controls are changed by the user.
        /// It refreshes the graphs accordingly.
        /// </summary>
        /// <param name="tabIndex">The tab</param>
        /// <param name="startYear">The start year</param>
        /// <param name="showYears">Number of years to show</param>
        public void GraphRefreshValueChanged(int tabIndex, decimal startYear, decimal showYears)
        {
            try
            {
                using (DataTable data = this.graphMetData)
                {
                    DateTime startDate = new DateTime(Convert.ToInt16(startYear), 1, 1);
                    DateTime endDate = new DateTime(Convert.ToInt16(startYear), 12, 31);
                    if (showYears > 1)
                    {
                        endDate = endDate.AddYears(Convert.ToInt16(showYears) - 1);
                    }

                    this.DisplayDetailedGraphs(data, tabIndex, startDate, endDate, false);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// This is called when the value of DropDown combo list containing sheet names is changed.
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="sheetName">The sheet name</param>
        public void ExcelSheetValueChanged(string fileName, string sheetName)
        {
            if (!string.IsNullOrEmpty(sheetName))
            {
                if ((this.weatherData.FullFileName != PathUtilities.GetAbsolutePath(fileName, this.explorerPresenter.ApsimXFile.FileName)) ||
                    (this.weatherData.ExcelWorkSheetName != sheetName))
                {
                    this.WriteTableAndSummary(fileName, sheetName);
                }
            }
        }

        /// <summary>
        /// Get data from the weather file and present it to the view as both a table and a summary
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="sheetName">The name of the sheet</param>
        private void WriteTableAndSummary(string filename, string sheetName = "")
        {
            // Clear any previous summary
            this.weatherDataView.Summarylabel = string.Empty;
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.Refresh();
            this.weatherDataView.GraphRainfall.Clear();
            this.weatherDataView.GraphRainfall.Refresh();
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            this.weatherDataView.GraphMonthlyRainfall.Refresh();
            this.weatherDataView.GraphTemperature.Clear();
            this.weatherDataView.GraphTemperature.Refresh();
            this.weatherDataView.GraphRadiation.Clear();
            this.weatherDataView.GraphRadiation.Refresh();
            this.graphMetData = new DataTable();
            if (filename != null)
            {
                this.weatherDataView.Filename = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
                try
                {
                    if (Path.GetExtension(filename) == ExcelUtilities.ExcelExtension)
                    {
                        // Extend height of Browse Panel to show Drop Down for Sheet names
                        this.weatherDataView.ShowExcelSheets(true);
                        if (this.sheetNames == null)
                        {
                            this.sheetNames = ExcelUtilities.GetWorkSheetNames(filename);
                            this.weatherDataView.ExcelSheetChangeClicked -= this.ExcelSheetValueChanged;
                            this.weatherDataView.PopulateDropDownData(this.sheetNames);
                            this.weatherDataView.ExcelSheetChangeClicked += this.ExcelSheetValueChanged;
                        }
                    }
                    else
                    {
                        // Shrink Browse Panel so that the sheet name dropdown doesn't show
                        this.weatherDataView.ShowExcelSheets(false);
                    }

                    (this.weatherDataView as TabbedMetDataView).WaitCursor = true;
                    try
                    {
                        this.weatherData.ExcelWorkSheetName = sheetName;
                        explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(weatherData, "FullFileName", PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName)));

                        using (DataTable data = this.weatherData.GetAllData())
                        {
                            this.dataStartDate = this.weatherData.StartDate;
                            this.dataEndDate = this.weatherData.EndDate;
                            this.WriteTable(data);
                            this.WriteSummary(data);
                            this.DisplayDetailedGraphs(data);
                        }

                        this.explorerPresenter.MainPresenter.ShowMessage(" ", Simulation.MessageType.Information);
                    }
                    finally
                    {
                        (this.weatherDataView as TabbedMetDataView).WaitCursor = false;
                        this.weatherData.CloseDataFile();
                    }
                }
                catch (Exception err)
                {
                    string message = err.Message;
                    message += "\r\n" + err.StackTrace;
                    this.weatherDataView.Summarylabel = err.Message;
                    this.explorerPresenter.MainPresenter.ShowError(err);
                }
            }

            // this.weatherDataView.Filename = PathUtilities.GetRelativePath(filename, this.explorerPresenter.ApsimXFile.FileName);
            this.weatherDataView.Filename = PathUtilities.GetAbsolutePath(filename, this.explorerPresenter.ApsimXFile.FileName);
            this.weatherDataView.ExcelWorkSheetName = sheetName;
        }

        /// <summary>Send the DataTable to the View</summary>
        /// <param name="data">The data set</param>
        private void WriteTable(DataTable data)
        {
            // format the data into useful columns
            if (data != null)
            {
                int siteIdx = data.Columns.IndexOf("site");
                if (siteIdx >= 0)
                {
                    data.Columns.RemoveAt(siteIdx);
                }

                // modLMC - 10/03/2016 - Add the Qmax (Max Radiation) column that we require for the graphs
                // This is done here so that we can use the "day" or "doy" column if it exists, as it will be quicker
                MetUtilities.CalcQmax(data, this.weatherData.Latitude);

                // modLMC - 10/03/2016 - Modified to use this new function, as some data has "doy" and not "day"
                int dayCol = data.Columns.IndexOf("day");
                int yearCol = data.Columns.IndexOf("year");

                if ((yearCol >= 0) && (dayCol >= 0))
                {
                    // add a new column for the date string
                    DataColumn dateCol = data.Columns.Add("Date", Type.GetType("System.String"));
                    dateCol.SetOrdinal(0);
                    yearCol++;    // moved along
                    dayCol++;

                    int yr, day;

                    // for each row in the grid
                    for (int r = 0; r < data.Rows.Count; r++)
                    {
                        yr = Convert.ToInt32(data.Rows[r][yearCol]);
                        day = Convert.ToInt32(data.Rows[r][dayCol]);
                        DateTime rowDate = new DateTime(yr, 1, 1);
                        rowDate = rowDate.AddDays(day - 1);                 // calc date
                        data.Rows[r][0] = rowDate.ToShortDateString();      // store in Date col
                    }

                    if (dayCol > yearCol)
                    {
                        data.Columns.RemoveAt(dayCol);
                        data.Columns.RemoveAt(yearCol);       // remove unwanted columns
                    }
                    else
                    {
                        data.Columns.RemoveAt(yearCol);       // remove unwanted columns
                        data.Columns.RemoveAt(dayCol);
                    }
                }

                this.graphMetData = data;
                this.weatherDataView.PopulateData(data);
            }
        }

        /// <summary>Format a summary string about the weather file</summary>
        /// <param name="table">The data set</param>
        private void WriteSummary(DataTable table)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("File name : " + this.weatherData.FileName);
            if (!string.IsNullOrEmpty(this.weatherData.ExcelWorkSheetName))
            {
                summary.AppendLine("Sheet Name: " + this.weatherData.ExcelWorkSheetName.ToString());
            }

            summary.AppendLine("Latitude  : " + this.weatherData.Latitude.ToString());
            summary.AppendLine("TAV       : " + string.Format("{0, 2:f2}", this.weatherData.Tav));
            summary.AppendLine("AMP       : " + string.Format("{0, 2:f2}", this.weatherData.Amp));
            summary.AppendLine("Start     : " + this.dataStartDate.ToShortDateString());
            summary.AppendLine("End       : " + this.dataEndDate.ToShortDateString());
            summary.AppendLine(string.Empty);

            if (table != null && table.Rows.Count > 0)
            {
                this.dataFirstDate = DataTableUtilities.GetDateFromRow(table.Rows[0]);
                this.dataLastDate = DataTableUtilities.GetDateFromRow(table.Rows[table.Rows.Count - 1]);

                TimeSpan diff = this.dataLastDate - this.dataFirstDate;

                // modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (diff.Days > 365)
                {
                    if (this.dataFirstDate.DayOfYear != 1)
                    {
                        this.dataFirstDate = new DateTime(this.dataFirstDate.Year + 1, 1, 1);
                    }
                }

                // modLMC - 16/03/2016 - don't change dates if data is within the same year
                if (this.dataFirstDate.Year != this.dataLastDate.Year)
                {
                    if (this.dataLastDate.Day != 31 || this.dataLastDate.Month != 12)
                    {
                        this.dataLastDate = new DateTime(this.dataLastDate.Year - 1, 12, 31);
                    }
                }

                double[] yearlyRainfall = MathUtilities.YearlyTotals(table, "Rain", this.dataFirstDate, this.dataLastDate);
                double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", this.dataFirstDate, this.dataLastDate);
                double[] monthlyMaxT = MathUtilities.AverageDailyTotalsForEachMonth(table, "maxt", this.dataFirstDate, this.dataLastDate);
                double[] monthlyMinT = MathUtilities.AverageDailyTotalsForEachMonth(table, "mint", this.dataFirstDate, this.dataLastDate);

                // what do we do if the date range is less than 1 year.
                // modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                if (monthlyRainfall.Length <= 12)
                {
                    this.monthsToDisplay = DataTableUtilities.GetDistinctMonthsasStrings(table, this.dataFirstDate, this.dataLastDate);
                }

                // long term average rainfall
                if (yearlyRainfall.Length != 0)
                {
                    double totalYearlyRainfall = MathUtilities.Sum(yearlyRainfall);
                    int numYears = this.dataLastDate.Year - this.dataFirstDate.Year + 1;
                    double meanYearlyRainfall = totalYearlyRainfall / numYears;
                    double stddev = MathUtilities.StandardDeviation(yearlyRainfall);

                    summary.AppendLine(string.Format("For years : {0} - {1}", this.dataFirstDate.Year, this.dataLastDate.Year));
                    summary.AppendLine("Long term average yearly rainfall : " + string.Format("{0,3:f2}mm", meanYearlyRainfall));
                    summary.AppendLine("Yearly rainfall std deviation     : " + string.Format("{0,3:f2}mm", stddev));

                    string title = string.Format("Long term average data for years : {0} - {1}", this.dataFirstDate.Year, this.dataLastDate.Year);

                    // modlmc - 15/03/2016 - modified to pass in the "Month" values, and they may/may not contain a full year.
                    this.PopulateSummaryGraph(
                                        title,
                                        this.monthsToDisplay,
                                        monthlyRainfall,
                                        monthlyMaxT,
                                        monthlyMinT);
                }

                this.weatherDataView.Summarylabel = summary.ToString();
            }
        }

        /// <summary>Sets the date range for the graphs, and calls the graph display functions</summary>
        /// <param name="table">The data set</param>
        private void DisplayDetailedGraphs(DataTable table)
        {
            if (table != null && table.Rows.Count > 0)
            {
                // By default, only do one year (the first year)
                DateTime endDate = new DateTime(this.dataFirstDate.Year, 12, 31);

                // by default, assume if not passed in, then we are displaying first tab (tab 0)
                this.DisplayDetailedGraphs(table, 0, this.dataFirstDate, endDate, true);
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data table</param>
        /// <param name="tabIndex">The index of the tab</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayDetailedGraphs(DataTable table, int tabIndex, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    switch (tabIndex)
                    {
                        case 2:     // Daily Rain
                            this.DisplayGraphDailyRain(table, startDate, endDate, true);
                            break;
                        case 3:     // Monthly Rain
                            this.DisplayGraphMonthlyRain(table, startDate, endDate, true);
                            break;
                        case 4:     // Temperature
                            this.DisplayGraphTemperature(table, startDate, endDate, true);
                            break;
                        case 5:     // Radiation
                            this.DisplayGraphRadiation(table, startDate, endDate, true);
                            break;
                    }

                    if (updateYears == true)
                    {
                        this.SetGraphControlsDefaultValues();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphDailyRain(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    // Need to be able to filter the table based on first date and last date, so that we can graph
                    // graph the daily values for rainfall, temperature and radiation
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyRain = DataTableUtilities.GetColumnAsDoubles(table, "rain", startDate, endDate);

                    string rainMessage = string.Empty;
                    if (dailyRain.Length != 0)
                    {
                        double totalYearlyRainfall = Math.Round(MathUtilities.Sum(dailyRain), 1);
                        rainMessage = "Total Rainfall for the year " + startDate.Year.ToString()
                                    + " is " + totalYearlyRainfall.ToString() + "mm.";

                        this.PopulateRainfallGraph(rainMessage, dailyDates, dailyRain);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphMonthlyRain(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    double[] monthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", startDate, endDate);

                    if (monthlyRainfall.Length != 0)
                    {
                        double[] avgMonthlyRainfall = MathUtilities.AverageMonthlyTotals(table, "rain", this.dataFirstDate, this.dataLastDate);
                        this.PopulateMonthlyRainfallGraph(
                                                       "Monthly Rainfall",
                                                        this.monthsToDisplay, 
                                                        monthlyRainfall, 
                                                        avgMonthlyRainfall);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphTemperature(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyMaxTemp = DataTableUtilities.GetColumnAsDoubles(table, "maxt", startDate, endDate);
                    double[] dailyMinTemp = DataTableUtilities.GetColumnAsDoubles(table, "mint", startDate, endDate);

                    if (dailyMaxTemp.Length != 0)
                    {
                        this.PopulateTemperatureGraph("Temperature", dailyDates, dailyMaxTemp, dailyMinTemp);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>This refreshes data being displayed on the graphs, based on the value of the startYear and showYear values  </summary>
        /// <param name="table">The data set</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="updateYears">Update the years</param>
        private void DisplayGraphRadiation(DataTable table, DateTime startDate, DateTime endDate, bool updateYears)
        {
            try
            {
                if (table != null && table.Rows.Count > 0)
                {
                    DateTime[] dailyDates = DataTableUtilities.GetColumnAsDates(table, "Date", startDate, endDate);
                    double[] dailyRain = DataTableUtilities.GetColumnAsDoubles(table, "rain", startDate, endDate);
                    double[] dailyRadn = DataTableUtilities.GetColumnAsDoubles(table, "radn", startDate, endDate);
                    double[] dailyMaxRadn = DataTableUtilities.GetColumnAsDoubles(table, "Qmax", startDate, endDate);

                    if (dailyRadn.Length != 0)
                    {
                        this.PopulateRadiationGraph("Radiation", dailyDates, dailyRain, dailyRadn, dailyMaxRadn);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to display Detailed Graphs due to insufficient data: " + e.Message.ToString());
            }
        }

        /// <summary>
        /// Sets the default values for the Numeric Updown spin controls used for displaying graph data (Start Year and Years to Show).
        /// </summary>
        private void SetGraphControlsDefaultValues()
        {
            // Options for new Min Year:  less than current, greater than current, and greater than max
            // if less, just set it first, then value, then max
            // else is greater than max, then do max first, then value set value to max, set min, then reset value
            // if greater than current but less than max, then set value first, then min, then max
            if (this.dataStartDate.Year < this.weatherDataView.GraphStartYearMinValue)
            {
                this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
            }
            else if (this.weatherDataView.GraphStartYearMinValue >= this.dataEndDate.Year)
            {
                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataEndDate.Year;
                this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
            }
            else  
            {
                // we are between our original range
                if (this.weatherDataView.GraphStartYearMinValue < this.dataStartDate.Year)
                {
                    this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                    this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                }
                else
                {
                    this.weatherDataView.GraphStartYearValue = this.dataStartDate.Year;
                    this.weatherDataView.GraphStartYearMinValue = this.dataStartDate.Year;
                }

                this.weatherDataView.GraphStartYearMaxValue = this.dataEndDate.Year;
            }
        }

        /// <summary>Create the monthly Summary chart</summary>
        /// <param name="title">The title</param>
        /// <param name="months">Array of months</param>
        /// <param name="monthlyRain">Monthly rainfall</param>
        /// <param name="monthlyMaxT">Monthly Maximum Temperatures</param>
        /// <param name="monthlyMinT">Monthly Minimum Temperatures</param>
        private void PopulateSummaryGraph(string title, string[] months, double[] monthlyRain, double[] monthlyMaxT, double[] monthlyMinT)
        {
            this.weatherDataView.GraphSummary.Clear();
            this.weatherDataView.GraphSummary.DrawBar(
                                      title,
                                      months,
                                      monthlyRain,
                                      Axis.AxisType.Bottom,
                                      Axis.AxisType.Left,
                                      Color.LightSkyBlue,
                                      true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers(
                                                     "Maximum Temperature",
                                                     months,
                                                     monthlyMaxT,
                                                     null,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Red,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphSummary.DrawLineAndMarkers(
                                                     "Minimum Temperature",
                                                     months,
                                                     monthlyMinT,
                                                     null,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Bottom, "Month", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphSummary.FormatAxis(Axis.AxisType.Right, "Temperature (oC)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphSummary.FormatTitle(title);
            this.weatherDataView.GraphSummary.Refresh();
        }

        /// <summary>Creates the Rainfall Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="rain">An array of Rainfall amounts for the Y Axis</param>
        private void PopulateRainfallGraph(string title, DateTime[] dates, double[] rain)
        {
            this.weatherDataView.GraphRainfall.Clear();
            this.weatherDataView.GraphRainfall.DrawBar(
                                                       title,
                                                       dates,
                                                       rain,
                                                       Axis.AxisType.Bottom,
                                                       Axis.AxisType.Left,
                                                       Color.LightSkyBlue,
                                                       false);

            this.weatherDataView.GraphRainfall.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRainfall.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRainfall.FormatTitle(title);
            this.weatherDataView.GraphRainfall.Refresh();
        }

        /// <summary>
        /// Displays the Monthly rainfall chart, which shows the current years rain (by month), and the long term average monthly rainfall, 
        /// based on all data in metfile
        /// </summary>
        /// <param name="title">The title</param>
        /// <param name="months">Array of months</param>
        /// <param name="monthlyRain">Monthly rain data</param>
        /// <param name="avgMonthlyRain">Average monthly rain</param>
        private void PopulateMonthlyRainfallGraph(string title, string[] months, double[] monthlyRain, double[] avgMonthlyRain)
        {
            this.weatherDataView.GraphMonthlyRainfall.Clear();
            if (months.Length == monthlyRain.Length)
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawBar(
                                                           title,
                                                           months,
                                                           monthlyRain,
                                                           Axis.AxisType.Bottom,
                                                           Axis.AxisType.Left,
                                                           Color.LightSkyBlue,
                                                           true);
            }

            if ((avgMonthlyRain.Length != 0) && (avgMonthlyRain.Length == monthlyRain.Length))
            {
                this.weatherDataView.GraphMonthlyRainfall.DrawLineAndMarkers(
                                                 "Long term average Rainfall",
                                                 months,
                                                 avgMonthlyRain,
                                                 null,
                                                 Axis.AxisType.Bottom,
                                                 Axis.AxisType.Left,
                                                 Color.Blue,
                                                 LineType.Solid,
                                                 MarkerType.None,
                                                 LineThicknessType.Normal,
                                                 MarkerSizeType.Normal,
                                                 true);
            }

            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphMonthlyRainfall.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphMonthlyRainfall.FormatTitle(title);
            this.weatherDataView.GraphMonthlyRainfall.Refresh();
        }

        /// <summary>Creates the Temperature Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="maxTemps">An array of Max Temperatures amounts for the Y Axis</param>
        /// <param name="minTemps">An array of Minimum Temperatures amounts for the Y Axis</param>
        private void PopulateTemperatureGraph(string title, DateTime[] dates, double[] maxTemps, double[] minTemps)
        {
            this.weatherDataView.GraphTemperature.Clear();
            this.weatherDataView.GraphTemperature.DrawLineAndMarkers(
                                                     "Maximum Temperature",
                                                     dates,
                                                     maxTemps,
                                                     null, 
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Left,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphTemperature.DrawLineAndMarkers(
                                                     "Minimum Temperature",
                                                     dates,
                                                     minTemps,
                                                     null,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Left,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphTemperature.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphTemperature.FormatAxis(Axis.AxisType.Left, "Temperature (oC)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphTemperature.FormatTitle(title);
            this.weatherDataView.GraphTemperature.Refresh();
        }

        /// <summary>Creates the Radiation Chart</summary>
        /// <param name="title">The title to display on the chart</param>
        /// <param name="dates">An array of Dates for the x Axis</param>
        /// <param name="rain">An array of Rainfall amounts for the Y Axis</param>
        /// <param name="radn">Radiation values</param>
        /// <param name="maxRadn">Max radiation values</param>
        private void PopulateRadiationGraph(string title, DateTime[] dates, double[] rain, double[] radn, double[] maxRadn)
        {
            this.weatherDataView.GraphRadiation.Clear();
            this.weatherDataView.GraphRadiation.DrawBar(
                                                       "Rainfall",
                                                       dates,
                                                       rain,
                                                       Axis.AxisType.Bottom,
                                                       Axis.AxisType.Left,
                                                       Color.LightSkyBlue,
                                                       true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers(
                                                     "Radiation",
                                                     dates,
                                                     radn,
                                                     null,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Blue,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);
            this.weatherDataView.GraphRadiation.DrawLineAndMarkers(
                                                     "Maximum Radiation",
                                                     dates,
                                                     maxRadn,
                                                     null,
                                                     Axis.AxisType.Bottom,
                                                     Axis.AxisType.Right,
                                                     Color.Orange,
                                                     LineType.Solid,
                                                     MarkerType.None,
                                                     LineThicknessType.Normal,
                                                     MarkerSizeType.Normal,
                                                     true);

            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Bottom, "Date", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Left, "Rainfall (mm)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatAxis(Axis.AxisType.Right, "Radiation (mJ/m2)", false, double.NaN, double.NaN, double.NaN);
            this.weatherDataView.GraphRadiation.FormatTitle(title);
            this.weatherDataView.GraphRadiation.Refresh();
        }
    }
}
