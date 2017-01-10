using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class is responsible to all parsing operations.
    /// </summary>
    class Parse
    {
        /// <summary>
        /// a hash set dedicated to hold all the stop words for quick look up
        /// </summary>
        HashSet<string> stopWordsSet;

        /// <summary>
        /// ctor of the parser class - reading the stop words from the stop_words.txt file and inserting the to the hash set
        /// </summary>
        public Parse()
        {
            stopWordsSet = new HashSet<string>();
        }

        public void stopWordSetInit()
        {
            string stopWordsPath = Properties.Settings.Default.sourceFilesPath + "\\stop_words.txt";
            if (File.Exists(stopWordsPath))
            {

                string[] stopWordsText = System.IO.File.ReadAllLines(stopWordsPath);

                foreach (string word in stopWordsText)
                {
                    stopWordsSet.Add(word);
                }
                stopWordsSet.Add("");
                stopWordsSet.Add("$");
                stopWordsSet.Add("$$$$$$$$");
            }
            else
            {
                throw new Exception("Stop word file was not found. please add the file to the corpus directory.");
            }
        }
        /// <summary>
        /// parsing a term by the given rules. returning the number of cell needed to be skipped to get to the next term and the parsed term or terms
        /// used when a term is more then one word.
        /// </summary>
        /// <param name="docText">the list conating the text</param>
        /// <param name="termPosition">the current index in the text list which is going to be parsed</param>
        /// <param name="returnedParsedTerm1">this is the me varible returing the parsed term, null if the term wasnt parsed</param>
        /// <param name="returnedParsedTerm2">optinal - some rules dictates to terms needed to be returns</param>
        /// <returns></returns>
        public int parseTerm(ref List<string> docText, int termPosition, out string returnedParsedTerm1, out string returnedParsedTerm2)
        {
            if (stopWordsSet.Count == 0)
                stopWordSetInit();
            string term = docText[termPosition];

            double number;
            int intNumber;
            if (term.Length > 2 && term.Substring(0, 3).Equals("id="))
            {
                returnedParsedTerm1 = null;
                returnedParsedTerm2 = null;
                return -1;
            }
            if (Double.TryParse(term, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out number))
            {
                if (number >= 1000000)
                {
                    string parsedTerm;
                    int parsedNum = (int)number / 1000000;
                    if (number % 1000000 != 0)
                    {
                        parsedTerm = number / 1000000 + "m";
                        if (docText[termPosition + 1].Equals("dollars"))
                        {
                            parsedTerm += " dollars";
                            returnedParsedTerm1 = parsedTerm;
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm, 1);//1M Dollars or 1.2M Dollars  
                            return 1;
                        }
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//1.2M
                        return 0;
                    }
                    else
                    {
                        parsedTerm = parsedNum + "m";
                        if (docText[termPosition + 1].Equals("dollars"))
                        {
                            parsedTerm += " dollars";
                            returnedParsedTerm1 = parsedTerm;
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm, 1);//1M Dollars or 1.2M Dollars                                              
                            return 1;
                        }
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//1M
                        return 0;
                    }
                }
                else
                {
                    string parsedTerm;
                    if (docText[termPosition + 1].Equals("million"))
                    {
                        parsedTerm = docText[termPosition] + "m";
                        if (docText[termPosition + 2].Equals("u.s.") && docText[termPosition + 3].Equals("dollars"))
                        {
                            returnedParsedTerm1 = parsedTerm + " dollars";
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm + " dollars", 1);//7M
                            return 3;
                        }
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//7M
                        return 1;
                    }
                    if (docText[termPosition + 1].ToLower().Equals("billion"))
                    {
                        parsedTerm = (int)(number * 1000) + "m";
                        if (docText[termPosition + 2].Equals("u.s.") && docText[termPosition + 3].Equals("dollars"))
                        {
                            returnedParsedTerm1 = parsedTerm + " dollars";
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm + " dollars", 1);//7M
                            return 3;
                        }
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//7000M
                        return 1;
                    }
                    if (docText[termPosition + 1].Equals("trillion"))
                    {
                        parsedTerm = (int)(number * 1000000) + "m";
                        if (docText[termPosition + 2].Equals("u.s.") && docText[termPosition + 3].Equals("dollars"))
                        {
                            returnedParsedTerm1 = parsedTerm + " dollars";
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm + " dollars", 1);//7M
                            return 3;
                        }
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//7000000M
                        return 1;
                    }
                    if (docText[termPosition + 1].Equals("percent") || docText[termPosition + 1].Equals("percentage"))
                    {
                        parsedTerm = docText[termPosition] + "%";
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//6%(percent or percentage) or 10.6%
                        return 1;
                    }
                    if (docText[termPosition + 1].Equals("dollars"))
                    {
                        parsedTerm = docText[termPosition] + " dollars";
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//7 Dollars ir 1.2 Dollars
                        return 1;
                    }
                    if ((docText[termPosition + 2].Equals("dollars") && docText[termPosition + 1].Contains("/")))
                    {
                        parsedTerm = docText[termPosition] + " " + docText[termPosition + 1] + " " + docText[termPosition + 2];
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);//7 3/4 Dollars
                        return 2;
                    }
                    if (!monthParse(docText[termPosition + 1]).Equals("0") && yearParse(docText[termPosition + 2]) != -1 && number <= 31)
                    {
                        parsedTerm = yearParse(docText[termPosition + 2]) + "-" + monthParse(docText[termPosition + 1]) + "-" + ddParser((int)number);

                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = yearParse(docText[termPosition + 2]).ToString();
                        //addTermToData(docNum, parsedTerm, 1);// month dd year
                        //addTermToData(docNum, yearParse(docText[termPosition + 2].ToLower()).ToString(), 1);
                        return 2;
                    }
                    if (!monthParse(docText[termPosition + 1]).Equals("0"))
                    {
                        parsedTerm = monthParse(docText[termPosition + 1]) + "-" + ddParser((int)number);
                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        //addTermToData(docNum, parsedTerm, 1);// month dd                  
                        return 1;
                    }
                    returnedParsedTerm1 = docText[termPosition];
                    returnedParsedTerm2 = null;
                    //addTermToData(docNum, docText[termPosition], 1);
                    return 0;
                }

            }
            else
            {
                string parsedTerm;
                if (term[0].Equals('$'))
                {
                    if ((Double.TryParse(term.Substring(1), NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out number)))
                    {
                        if (number < 1000000)
                        {
                            if (docText[termPosition + 1].Equals("million"))
                            {
                                parsedTerm = term.Substring(1) + "m" + " dollars";
                                returnedParsedTerm1 = parsedTerm;
                                returnedParsedTerm2 = null;
                                //addTermToData(docNum, parsedTerm, 1);//100M Dollars
                                return 1;
                            }
                            if (docText[termPosition + 1].Equals("billion"))
                            {
                                parsedTerm = (int)(number * 1000) + "m" + " dollars";
                                returnedParsedTerm1 = parsedTerm;
                                returnedParsedTerm2 = null;
                                //addTermToData(docNum, parsedTerm, 1);//1000M Dollars
                                return 1;
                            }
                            if (docText[termPosition + 1].Equals("trillion"))
                            {
                                parsedTerm = (int)(number * 1000000) + "m" + " dollars";
                                returnedParsedTerm1 = parsedTerm;
                                returnedParsedTerm2 = null;
                                //addTermToData(docNum, parsedTerm, 1);//1000000Dollars
                                return 1;
                            }
                            parsedTerm = term.Substring(1) + " dollars";
                            returnedParsedTerm1 = parsedTerm;
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm, 1);//1 Dollars or 1.4 Dollars
                            return 0;
                        }
                        else//>1000000
                        {
                            int parsedNum = (int)number / 1000000;
                            if (number % 1000000 != 0)
                            {
                                parsedTerm = number / 1000000 + "M dollars";
                                returnedParsedTerm1 = parsedTerm;
                                returnedParsedTerm2 = null;
                                //addTermToData(docNum, parsedTerm, 1);//$ -> 450.5M Dollars
                                return 0;
                            }
                            else
                            {
                                parsedTerm = parsedNum + "M dollars";
                                returnedParsedTerm1 = parsedTerm;
                                returnedParsedTerm2 = null;
                                //addTermToData(docNum, parsedTerm, 1);//$ -> 450M Dollars
                                return 0;
                            }
                        }
                    }

                }

                if (term[term.Length - 1].Equals('m'))
                {
                    if (Double.TryParse(term.Substring(0, term.Length - 1), NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out number))
                    {
                        parsedTerm = term.Substring(0, term.Length - 1) + "m";
                        if (docText[termPosition + 1].Equals("dollars"))
                        {
                            returnedParsedTerm1 = parsedTerm + " dollars";
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm + " Dollars", 1);//26m Dollars -> 26M Dollars
                            return 1;
                        }
                    }
                }
                if (term.Length >= 2 && term.Substring(term.Length - 2, 2).Equals("bn"))
                {
                    if (Double.TryParse(term.Substring(0, term.Length - 2), NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out number))
                    {
                        parsedTerm = (int)number * 1000 + "m";
                        if (docText[termPosition + 1].Equals("dollars"))
                        {
                            returnedParsedTerm1 = parsedTerm + " dollars";
                            returnedParsedTerm2 = null;
                            //addTermToData(docNum, parsedTerm + " Dollars", 1);//26bn Dollars -> 26000M Dollars
                            return 1;
                        }
                    }
                }
                if (term.Length >= 2 && term.Substring(term.Length - 2, 2).Equals("'s"))
                {

                    returnedParsedTerm1 = term.Substring(0, term.Length - 2);
                    returnedParsedTerm2 = null;
                    //addTermToData(docNum, parsedTerm + " Dollars", 1);//26bn Dollars -> 26000M Dollars
                    return 0;

                }
                if (term.Length >= 2 && term.Substring(term.Length - 2, 2).Equals("mm") && term.Substring(term.Length - 2, 2).Equals("km") && term.Substring(term.Length - 2, 2).Equals("cm"))
                {
                    if (Int32.TryParse(term.Substring(0, term.Length - 2), NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out intNumber))
                    {

                        if (term.Substring(term.Length - 2, 2).Equals("mm"))
                        {
                            parsedTerm = intNumber + " millimeter";

                        }
                        else if (term.Substring(term.Length - 2, 2).Equals("cm"))
                        {
                            parsedTerm = intNumber + " centimeter";

                        }
                        else
                        {
                            parsedTerm = intNumber + " kilometer";

                        }

                        returnedParsedTerm1 = parsedTerm;
                        returnedParsedTerm2 = null;
                        return 2;

                    }
                }
                if (term.Length >= 2 && term.Substring(term.Length - 2, 2).Equals("th") && term.Substring(term.Length - 2, 2).Equals("st") && term.Substring(term.Length - 2, 2).Equals("nd"))
                {
                    if (Int32.TryParse(term.Substring(0, term.Length - 2), NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out intNumber))
                    {
                        if (number <= 31 && !monthParse(docText[termPosition + 1]).Equals("0") && yearParse(docText[termPosition + 2]) != -1)
                        {
                            parsedTerm = yearParse(docText[termPosition + 2]) + "-" + monthParse(docText[termPosition + 1]) + "-" + ddParser(intNumber);

                            returnedParsedTerm1 = parsedTerm;
                            returnedParsedTerm2 = yearParse(docText[termPosition + 2]).ToString();
                            //addTermToData(docNum, parsedTerm, 1);//ddth month year
                            //addTermToData(docNum, yearParse(docText[termPosition + 2].ToLower()).ToString(), 1);
                            return 2;
                        }
                    }
                }
                if (!monthParse(docText[termPosition]).Equals("0") && docText[termPosition + 1].Length > 1 && docText[termPosition + 1][docText[termPosition + 1].Length - 1].Equals(',') && Int32.TryParse(docText[termPosition + 1].Substring(0, docText[termPosition + 1].Length - 1), out intNumber) && yearParse(docText[termPosition + 2]) != -1)
                {

                    parsedTerm = yearParse(docText[termPosition + 2]) + "-" + monthParse(docText[termPosition]) + "-" + ddParser(intNumber);

                    returnedParsedTerm1 = parsedTerm;
                    returnedParsedTerm2 = yearParse(docText[termPosition + 2]).ToString();
                    //addTermToData(docNum, parsedTerm, 1);// month dd, year
                    //addTermToData(docNum, yearParse(docText[termPosition + 2].ToLower()).ToString(), 1);
                    return 2;


                }
                if (!monthParse(docText[termPosition]).Equals("0") && Int32.TryParse(docText[termPosition + 1], out intNumber) && docText[termPosition + 1].Length == 2 && intNumber <= 31)
                {

                    parsedTerm = monthParse(docText[termPosition]) + "-" + ddParser(intNumber);
                    returnedParsedTerm1 = parsedTerm;
                    returnedParsedTerm2 = null;
                    //addTermToData(docNum, parsedTerm, 1);// month dd                   
                    return 1;
                }
                if (!monthParse(docText[termPosition]).Equals("0") && yearParse(docText[termPosition + 1]) != -1)
                {
                    parsedTerm = yearParse(docText[termPosition + 1]) + "-" + monthParse(docText[termPosition]);
                    returnedParsedTerm1 = parsedTerm;
                    returnedParsedTerm2 = yearParse(docText[termPosition + 1]).ToString();
                    //addTermToData(docNum, parsedTerm, 1);// month year  
                    //addTermToData(docNum, yearParse(docText[termPosition + 1].ToLower()).ToString(), 1);
                    return 1;
                }

                if (docText[termPosition].Equals("between") && Double.TryParse(docText[termPosition + 1], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out number) && Double.TryParse(docText[termPosition + 2], NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-GB"), out number))
                {
                    returnedParsedTerm1 = docText[termPosition] + " " + docText[termPosition + 1] + " " + "and" + " " + docText[termPosition + 2];
                    returnedParsedTerm2 = null;
                    //addTermToData(docNum, docText[termPosition] + " " + docText[termPosition + 1] + " " + docText[termPosition + 2] + " " + docText[termPosition + 3], 1);
                    return 2;
                }

            }
            char[] a = { '\\', ',', '/', ':', '?', '*' };
            int b = term.IndexOfAny(a);
            if (b != -1)
            {
                int c;
                if (Int32.TryParse(term.Substring(0, b), out c) && Int32.TryParse(term.Substring(0, b), out c))
                {
                    returnedParsedTerm1 = term;
                    returnedParsedTerm2 = null;
                    return 0;
                }
                docText.Insert(termPosition + 1, term.Substring(0, b));
                docText.Insert(termPosition + 2, term.Substring(b + 1));
                returnedParsedTerm1 = null;
                returnedParsedTerm2 = null;
                return -1;
            }
            returnedParsedTerm1 = term;
            returnedParsedTerm2 = null;
            return -1;
        }
        /// <summary>
        /// parsing a month to its relevent number
        /// </summary>
        /// <param name="monthSource">the string describing a month</param>
        /// <returns></returns>
        string monthParse(string monthSource)
        {
            string month;
            switch (monthSource.ToLower())
            {
                case "january":
                case "jan":
                    month = "01";
                    break;
                case "february":
                case "feb":
                    month = "02";
                    break;
                case "march":
                case "mar":
                    month = "03";
                    break;
                case "april":
                case "apr":
                    month = "04";
                    break;
                case "may":
                    month = "05";
                    break;
                case "june":
                case "jun":
                    month = "06";
                    break;
                case "july":
                case "jul":
                    month = "07";
                    break;
                case "august":
                case "aug":
                    month = "08";
                    break;
                case "september":
                case "sep":
                    month = "09";
                    break;
                case "october":
                case "oct":
                    month = "10";
                    break;
                case "november":
                case "nov":
                    month = "11";
                    break;
                case "december":
                case "dec":
                    month = "12";
                    break;
                default:
                    month = "0";
                    break;
            }
            return month;
        }
        /// <summary>
        /// parsing a year
        /// </summary>
        /// <param name="yearSource">the string describing a year</param>
        /// <returns></returns>
        int yearParse(string yearSource)
        {
            int year;
            if ((Int32.TryParse(yearSource, out year)))
            {
                if (year <= 16)
                {
                    return 2000 + year;
                }
                if (year > 16 && year <= 99)
                    return 1900 + year;
                else
                    return year;
            }
            return -1;
        }
        /// <summary>
        /// parsing a the day part of the date format with two digits format
        /// </summary>
        /// <param name="dd"></param>
        /// <returns></returns>
        string ddParser(int dd)
        {
            if (dd < 10)
                return "0" + dd;
            else
                return dd.ToString();
        }
        /// <summary>
        /// This methods checks if a word is a stop word
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int checkForStopWord(string word)
        {
            if (stopWordsSet.Contains(word))
            {
                return 1;//is stop word
            }
            else
                return 0;// is not stop word
        }
        /// <summary>
        /// This methods clean the term from all unimportant signs to help the parsing process
        /// </summary>
        /// <param name="uncutTerm"></param>
        /// <returns></returns>
        public string cutAllsigns(string uncutTerm)
        {
            char[] charStart = new char[] { '%', '`' };
            char[] charArray = new char[] { '~', '_', '#', '|', '\'', '(', '"', '.', ',', '!', ':', '?', '"', '/', '\\', ';', '-', '=', '[', '{', '*', '+', '&', '<', '`', '[', '.', ',', '!', ':', '?', '"', '\'', '/', '\\', ';', ')', '-', '\'', '=', ']', '}', '*', '+', '&', '>' };
            uncutTerm = uncutTerm.TrimStart(charStart);
            return uncutTerm.Trim(charArray);
        }

    }
}
