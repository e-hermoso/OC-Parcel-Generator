using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ParcelCreation;

[assembly: CommandClass(typeof(CrxApp.MainProgram))]
[assembly: ExtensionApplication(null)]

namespace CrxApp
{
    class MainProgram
    {
        [CommandMethod("AMPARCEL")]
        public void CreaeParcel()
        {
            // Read input parameters from JSON file
            List<RowDataClass> inputParams = JsonConvert.DeserializeObject<List<RowDataClass>>(File.ReadAllText("traversedata-test.json"));


            System.Diagnostics.Debug.WriteLine("This is a log");
        }
        public void ReadCSVFile()
        {
            using(var reader = new StreamReader("AlisoCreekDownStreamBoundary2020-2.txt"))
            {
                int lineNumberInFile = 0;

                // Collect the number of rows that passed in the csv file based on the proper format.
                List<RowDataClass> rowCollection = new List<RowDataClass>();

                // Collect the number of of erros found in the csv file that are not in the proper format.
                List<Dictionary<string, object>> csvFormatErrorList = new List<Dictionary<string, object>>();

                while (!reader.EndOfStream)
                {
                    lineNumberInFile += 1;

                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    // Validate the text file as a csv file                
                    int numberOfIndex = values.Length;
                    
                    if(numberOfIndex == 5)
                    {
                        // Determine if the first row are headers 
                        if(lineNumberInFile == 1)
                        {
                            // Check if all indexes contain only letters to determin if the row is a header
                            int countHeaders = values.Select(x => Regex.IsMatch(x, @"^[a-zA-Z\s]*$")).ToList().Where(x => x == true).Count();

                            if(countHeaders == 5)
                            {
                                // This is a header
                            }
                            else
                            {
                                // Check if each index is in the proper format [int,double,double,double,string]
                                Dictionary<string, object> rowValues = checkRowFormat(values.ToList(), lineNumberInFile);

                                // Analyze each index value in description column and store them into a list.
                                rowCollection.Add(checkKeyDescription(rowValues));
                            }
                        }
                        else
                        {
                            // Check if each index is in the proper format [int,double,double,double,string]
                            Dictionary<string, object> rowValues = checkRowFormat(values.ToList(), lineNumberInFile);

                            // Analyze each index value in description column and store them into a list.
                            rowCollection.Add(checkKeyDescription(rowValues));
                        }
                    }
                    else
                    {
                        // CSV file is not in the proper format must be 5 columns : [PointNumber, Easting, Northing, Elevation, Description]
                        Dictionary<string, object> formatErrorDict = new Dictionary<string, object>();
                        formatErrorDict.Add("ErrorRow", String.Format("Found error in row number {0}", lineNumberInFile.ToString()));
                        formatErrorDict.Add("ErrorMsg", "Please make sure the csv file is in the proper format(Point Number, Northing, Easting, Elevation, Description) with no extra commas.\n Example: 1001,6127164.4032,2176455.7698,0.0,BOUNDARY");
                        csvFormatErrorList.Add(formatErrorDict);
                    }
                }

                // If any error is found do not continue to furthor anlyze the csv file.
                if (!csvFormatErrorList.Any())
                {
                    // Analyze the sequence of the point.
                    Dictionary<string,object> seqLineCodeResult = CheckRowsLineCodeSeq(rowCollection);

                    if((string)seqLineCodeResult["LineCodeFinalResult"] == "Pass")
                    {
                        Dictionary<string, object> reportResults = new Dictionary<string, object>();
                        reportResults.Add("ParcelCreator", "Pass");
                        reportResults.Add("ErrorType", "None");
                        reportResults.Add("ErrorTypeMsg", "None");
                        reportResults.Add("Result", seqLineCodeResult);
                        reportResults.Add("RowData", rowCollection);

                        using (var writer_1 = File.CreateText("traversedata.json"))
                        {

                            string strResultJson = JsonConvert.SerializeObject(rowCollection, Formatting.Indented);
                            writer_1.WriteLine(strResultJson);
                        }
                    }
                    else
                    {
                        Dictionary<string, object> reportResults = new Dictionary<string, object>();
                        reportResults.Add("ParcelCreator", "Fail");
                        reportResults.Add("ErrorType", "Sequence");
                        reportResults.Add("ErrorTypeMsg", "LineCode Sequence Error");
                        reportResults.Add("Result", seqLineCodeResult);
                    }

                }
                else
                {
                    Dictionary<string, object> reportResults = new Dictionary<string, object>();
                    reportResults.Add("ParcelCreator", "Fail");
                    reportResults.Add("ErrorType", "Format");
                    reportResults.Add("ErrorTypeMsg", "CSV Format Error");
                    reportResults.Add("Result", csvFormatErrorList);
                }
            }
        }
        public static Dictionary<string, object> checkRowFormat(List<string> listOfValues, int lineNumber)
        {
            Dictionary<string, object> dictResult = new Dictionary<string, object>();
            
            bool checkPointNumberValue = Int32.TryParse(listOfValues[0],out int pointNumber);
            bool checkEastingValue = double.TryParse(listOfValues[1],out double eastingValue);
            bool checkNorthingValue = double.TryParse(listOfValues[2],out double northingValue);
            bool checkElevationValue = double.TryParse(listOfValues[3], out double elevationValue);
            bool checkDescription = listOfValues[4] is string;

            if(checkPointNumberValue && checkEastingValue && checkNorthingValue && checkElevationValue && checkDescription)
            {
                List<object> reFormatValues = new List<object>();
                reFormatValues.Add(pointNumber);
                reFormatValues.Add(eastingValue);
                reFormatValues.Add(northingValue);
                reFormatValues.Add(elevationValue);
                reFormatValues.Add(listOfValues[4]);

                dictResult.Add("FormatCheck", "Pass");
                dictResult.Add("FormatResult", reFormatValues);
                dictResult.Add("FormatResultMsg", "CSV Format pass");
            }
            else if (!checkPointNumberValue)
            {
                List<object> reFormatValues = new List<object>();

                dictResult.Add("FormatCheck", "Fail");
                dictResult.Add("FormatResult", null);
                dictResult.Add("FormatResultMsg", String.Format("Error in Line Number {0}.\nPoint number is not in the correct format.", lineNumber));
            }
            else if (checkEastingValue && checkNorthingValue && !checkElevationValue)
            {

                dictResult.Add("FormatCheck", "Fail");
                dictResult.Add("FormatResult", null);
                dictResult.Add("FormatResultMsg", String.Format("Error in Line Number {0}.\nElevation is not in the correct format.", lineNumber));
            }
            else if (!checkEastingValue && checkNorthingValue && checkElevationValue)
            {

                dictResult.Add("FormatCheck", "Fail");
                dictResult.Add("FormatResult", null);
                dictResult.Add("FormatResultMsg", String.Format("Error in Line Number {0}.\nEasting is not in the correct format.", lineNumber));

            }
            else if (checkEastingValue && !checkNorthingValue && checkElevationValue)
            {
                dictResult.Add("FormatCheck", "Fail");
                dictResult.Add("FormatResult", null);
                dictResult.Add("FormatResultMsg", String.Format("Error in Line Number {0}.\nNorthing is not in the correct format.", lineNumber));
            }
            else
            {
                dictResult.Add("FormatCheck", "Fail");
                dictResult.Add("FormatResult", null);
                dictResult.Add("FormatResultMsg", String.Format("Error in Line Number {0}.\nDescription is not in the correct format.", lineNumber));
            }
            return dictResult;
        }
        public static Dictionary<string,string> analyzeDescription(string descriptionValue)
        {
            Dictionary<string, string> analyzedResult = new Dictionary<string, string>();
            
            // Dictionary of regular expression data for description.
            Dictionary<string, string> lineCodeRegex = new Dictionary<string, string>()
            {
                {"beginingFig", "^B$|(?<=(\\s))B(\\s)|(?<=(\\s))B$|^B(?=(\\s))" },
                {"endFig", "^E$|(?<=(\\s))E(\\s)|(?<=(\\s))E$|^E(?=(\\s))" },
                {"startCurve","^BC$|(?<=(\\s))BC(\\s)|(?<=(\\s))BC$|^BC(?=(\\s))"},
                {"endCurve","^EC$|(?<=(\\s))EC(\\s)|(?<=(\\s))EC$|^EC(?=(\\s))"},
                {"arcDirection","^(CW|CCW)$|(?<=(\\s))(CW|CCW)(\\s)|(?<=(\\s))(CW|CCW)$|^(CW|CCW)(?=(\\s))"},
                {"radius","^(R\\d+.?\\d*)|(?<=(\\s+))(R\\d+\\.?\\d*\\s*)|(?<=(\\s))(R\\d+\\.?\\d*)$"},
            };

            //Analyze description for Line Code
            //Match matchBegineFig = Regex.Match(descriptionValue, lineCodeRegex["beginingFig"], RegexOptions.IgnoreCase);
            //Match matchEndFig = Regex.Match(descriptionValue, lineCodeRegex["endFig"], RegexOptions.IgnoreCase);
            //Match matchArcDirection = Regex.Match(descriptionValue, lineCodeRegex["arcDirection"], RegexOptions.IgnoreCase);
            //Match matchRadius = Regex.Match(descriptionValue, lineCodeRegex["radius"], RegexOptions.IgnoreCase);

            foreach(KeyValuePair<string,string> item in lineCodeRegex)
            {
                Match matchDescription = Regex.Match(descriptionValue, item.Value, RegexOptions.IgnoreCase);

                if (matchDescription.Success)
                {
                    analyzedResult.Add(item.Key, matchDescription.Value);
                }
                else
                {
                    analyzedResult.Add(item.Key, "None");
                }
            }

            return analyzedResult;
        }
        private static RowDataClass checkKeyDescription(Dictionary<string, object> descriptionValues)
        {
            string errorMsg = "";

            if (descriptionValues["FormatCheck"].ToString() == "Pass")
            {
                RowDataClass rowData = new RowDataClass();
                rowData.pointNumber = (int)((List<object>)descriptionValues["FormatResult"])[0];
                rowData.start_x = (double)((List<object>)descriptionValues["FormatResult"])[1];
                rowData.start_y = (double)((List<object>)descriptionValues["FormatResult"])[2];
                rowData.start_z = (double)((List<object>)descriptionValues["FormatResult"])[3];

                var descriptionResult = (string)((List<object>)descriptionValues["FormatResult"])[4];

                // Analyze Description and look for key words
                Dictionary<string, string> analyzedDescription = analyzeDescription(descriptionResult);

                // Set pointCheck to null.
                rowData.pointCheck = null;

                // Check if the start and end of a figure was determined.
                if (analyzedDescription["beginingFig"] != "None")
                {
                    rowData.startFig = true;
                }
                else
                {
                    rowData.startFig = false;
                }

                if (analyzedDescription["endFig"] != "None")
                {
                    rowData.endFig = true;
                }
                else
                {
                    rowData.endFig = false;
                }

                // Check if the point is the start of the curve
                if (analyzedDescription["startCurve"] != "None")
                {

                    rowData.startCurve = true;

                    // Check for Radius
                    if (analyzedDescription["radius"] != "None")
                    {
                        Match radMatch = Regex.Match(analyzedDescription["radius"], @"\d+\.?\d+");

                        //radMatch.Success? rowData.radius = Convert.ToDouble(radMatch.Value) : rowData.radius = 0;
                        if (radMatch.Success)
                        {
                            rowData.radius = Convert.ToDouble(radMatch.Value);
                        }
                    }
                    else
                    {
                        // Report Error for Radius
                        rowData.pointCheck = false;
                        errorMsg += "No Radius found.";
                    }

                    // Check for arc direction
                    if (analyzedDescription["arcDirection"].Trim() == "CCW")
                    {
                        rowData.isCounterClockWise = true;
                    }
                    else if (analyzedDescription["arcDirection"].Trim() == "CW")
                    {
                        rowData.isCounterClockWise = false;
                    }
                    else
                    {
                        // Report Error for arc direction
                        rowData.pointCheck = false;
                        errorMsg += " No arc direction found.";
                    }

                    // Check if the start point of an arc contins all the data needed to create an arc
                    if (rowData.pointCheck is bool)
                    {
                        if((bool)rowData.pointCheck == false)
                        {
                            errorMsg += " Include both radius and arc direction to the start of the curve in the description.";
                            errorMsg += "\nExample: BC R10.25 CCW";
                        }
                    }
                    else
                    {
                        // Begining of point on curve description passed.
                        rowData.pointCheck = true;
                    }

                }
                else
                {
                    rowData.startCurve = false;
                }

                // Check if the point is the end of the curve
                if (analyzedDescription["endCurve"] != "None")
                {
                    // Check end point description does not contain any begining of the point data
                    if (!rowData.startCurve)
                    {
                        // Ending of a point on curve description passed.
                        rowData.pointCheck = true;
                        rowData.endCurve = true;
                    }
                    else
                    {
                        rowData.pointCheck = false;
                        rowData.endCurve = false;
                        errorMsg += "Cannot contain BC and EC line code to the same point.";
                    }
                }
                else
                {
                    rowData.endCurve = false;
                }

                // If pointCheck is null then no other aditional requirment is needed for description
                if (rowData.pointCheck is null)
                {
                    rowData.pointCheck = true;
                }

                // Add error message
                rowData.errorMsg = errorMsg;

                //rowCollection.Add(rowData);

                return rowData;
            }
            else
            {
                RowDataClass rowData = new RowDataClass();
                rowData.pointCheck = false;

                errorMsg += "File is not in the proper format must be in (PENZD)";
                rowData.errorMsg = errorMsg;
                //rowCollection.Add(rowData);

                return rowData;
            }
        }
        // Check if the sequence of LineCode in the description column are correct.
        private Dictionary<string, object> CheckRowsLineCodeSeq(List<RowDataClass> rowData)
        {

            Dictionary<string, object> sequenceResult = new Dictionary<string, object>();

            // Check if all the row of points passed.
            bool failedRows = rowData.Where(e => (bool)e.pointCheck == false).ToList().Any();
            if (!failedRows)
            {
                sequenceResult.Add("PointCheckResult", "Pass");
                sequenceResult.Add("PointCheckResultMsg", "No errors found in row.");
            }
            else
            {
                sequenceResult.Add("PointCheckResult", "Fail");
                sequenceResult.Add("PointCheckResultMsg", "Found a failed row in csv file.");
                
            }

            // Check if linecode B and E show up the same number of times.
            bool checkNumberOfFigures = CheckLineCodeMatch(rowData, "Figures");
            if (checkNumberOfFigures)
            {
                sequenceResult.Add("FigureCheckResult", "Pass");
                sequenceResult.Add("FigureCheckResultMsg", "Figures are closed.");
            }
            else
            {
                sequenceResult.Add("FigureCheckResult", "Fail");
                sequenceResult.Add("FigureCheckResultMsg", "Figures are not closed.");
            }

            // Check if BC and EC show up the same number of times.
            bool checkNumberOfCurves = CheckLineCodeMatch(rowData, "Arc");
            if (checkNumberOfCurves)
            {
                sequenceResult.Add("CurveCheckResult", "Pass");
                sequenceResult.Add("CurveCheckResultMsg", "Start and End Curves created.");
            }
            else
            {
                sequenceResult.Add("CurveCheckResult", "Fail");
                sequenceResult.Add("CurveCheckResultMsg", "Cannot create curve; start and end curve is not provided.");
            }

            // Check if B and E are in sequencial order.
            bool checkFigureSequence = CheckStartEndFigSeq(rowData);
            if (checkFigureSequence)
            {
                sequenceResult.Add("FigCheckSeq", "Pass");
                sequenceResult.Add("FigCheckSeqMsg", "the linecode for figures are in the correct sequence.");
            }
            else
            {
                sequenceResult.Add("FigCheckSeq", "Fail");
                sequenceResult.Add("FigCheckSeqMsg", "the linecode for figures are not in the correct sequence.");
            }

            // Check if BC and EC are in sequencial order.
            bool checkCurveSequence = CheckStartEndCurveSeq(rowData);
            if (checkCurveSequence)
            {
                sequenceResult.Add("CurveCheckSeq", "Pass");
                sequenceResult.Add("CurveCheckSeqMsg", "the linecode for curves are in the correct sequence.");
            }
            else
            {
                sequenceResult.Add("CurveCheckSeq", "Fail");
                sequenceResult.Add("CurveCheckSeqMsg", "the linecode for curves are not in the correct sequence.");
            }

            // Check if all the sequential conditions passed.
            var finalResult = sequenceResult.Where(x => (string)x.Value == "Fail").ToList().Any();
            if (!finalResult)
            {
                sequenceResult.Add("LineCodeFinalResult", "Pass");
            }
            else
            {
                sequenceResult.Add("LineCodeFinalResult", "Fail");
            }

            return sequenceResult;
        }
        // Check if the linecode for curves, figures, etc... show up evenly.
        // Figure out a better alternative for this mehtod.(TBD)
        private static bool CheckLineCodeMatch(List<RowDataClass> rowData, string keyWord)
        {
            List<int> countValues = new List<int>();

            if (keyWord == "Figures")
            {
                countValues.Add(rowData.FindAll(e => e.startFig == true).Count());
                countValues.Add(rowData.FindAll(e => e.endFig == true).Count());

                // if all the numbers are the same then the linecode in csv file passed
                // bool asdf = countValues.Any(x => x != countValues[0]);
                if (!countValues.Any(x => x != countValues[0]))
                {
                    countValues = null;
                    return true;
                }
                else
                {
                    countValues = null;
                    return false;
                }
            }
            else if (keyWord == "Arc")
            {
                countValues.Add(rowData.FindAll(e => e.startCurve == true).Count());
                countValues.Add(rowData.FindAll(e => e.endCurve == true).Count());

                // if all the numbers are the same then the linecode in csv file passed
                // bool asdf = countValues.Any(x => x != countValues[0]);
                if (!countValues.Any(x => x != countValues[0]))
                {
                    countValues = null;
                    return true;
                }
                else
                {
                    countValues = null;
                    return false;
                }
            }
            else
            {
                countValues = null;
                return false;
            }
        }

        // Check if the start and end figure are provided in a sequential order.
        private static bool CheckStartEndFigSeq(List<RowDataClass> rows)
        {
            int countFigs = 0;

            Dictionary<int, string> figureDict = new Dictionary<int, string>();
            
            foreach(RowDataClass  row in rows)
            {
                if (row.startFig)
                {
                    countFigs += 1;
                    figureDict.Add(countFigs, "B");
                }
                else if (row.endFig)
                {
                    countFigs += 1;
                    figureDict.Add(countFigs, "E");
                }
            }

            // Get all Begining figures "B"
            var oddValues = figureDict.Where(x => x.Key % 2 != 0).ToList();
            bool falseStartingFigure = oddValues.Any(x => x.Value != "B");

            // Get all Ending figures "E"
            var evenValues = figureDict.Where(x => x.Key % 2 == 0).ToList();
            bool falseEndingFigure = evenValues.Any(x => x.Value != "E");

            // If falseStartingFigure and falseEndingFigure are true then
            // the linecode figures are not in sequential order
            if(falseStartingFigure || falseEndingFigure)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
        // Check if the start and end of the curve are provided in a sequential order.
        private static bool CheckStartEndCurveSeq(List<RowDataClass> rows)
        {
            int countCurveLineCode = 0;

            Dictionary<int, string> curveDict = new Dictionary<int, string>();

            foreach (RowDataClass row in rows)
            {
                if (row.startCurve)
                {
                    countCurveLineCode += 1;
                    curveDict.Add(countCurveLineCode, "BC");
                }
                else if (row.endCurve)
                {
                    countCurveLineCode += 1;
                    curveDict.Add(countCurveLineCode, "EC");
                }
            }

            // Get all Begining figures "B"
            var oddValues = curveDict.Where(x => x.Key % 2 != 0).ToList();
            bool falseStartingCurve = oddValues.Any(x => x.Value != "BC");

            // Get all Ending figures "E"
            var evenValues = curveDict.Where(x => x.Key % 2 == 0).ToList();
            bool falseEndingCurve = evenValues.Any(x => x.Value != "EC");

            // If falseStartingFigure and falseEndingFigure are true then
            // the linecode figures are not in sequential order
            if (falseStartingCurve || falseEndingCurve)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public class InputParams
        {
            public string TextFile { get; set; }
            public string OutputDWG { get; set; }
        }
    }
}
