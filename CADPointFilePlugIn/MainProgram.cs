using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public void ReadCSVFile()
        {
            using(var reader = new StreamReader("AlisoCreekDownStreamBoundary2020-2.txt"))
            {
                int lineNumberInFile = 0;
                List<RowDataClass> rowCollection = new List<RowDataClass>();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    // Validate the text file as a csv file                
                    int numberOfIndex = values.Length;
                    
                    if(numberOfIndex == 5)
                    {
                        lineNumberInFile += 1;

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
                }
                System.Console.WriteLine("asdfs");
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
                        Match radMatch = Regex.Match(analyzedDescription["radius"], @"\d+\.?\d+?");

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
                    if ((bool)rowData.pointCheck == false)
                    {
                        errorMsg += " Include both radius and arc direction to the start of the curve in the description.";
                        errorMsg += "\nExample: BC R10.25 CCW";
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
    }
}
