using CognitiveFunction.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using Microsoft.Azure.WebJobs;

namespace CognitiveFunction.Services
{
    public class OCRHelper
    {
        public static List<TextExtract> RetrieveAllSearchTextKeyFields(string formType, ExecutionContext executionContext)
        {
            //json template path
            string file = string.Empty;

            switch (formType)
            {
                case "Classified Messages":
                    file = File.ReadAllText($"{ Directory.GetParent(executionContext.FunctionDirectory).FullName}\\Resources\\ClassifiedMessages.json");
                    break;
                case "JFK Identification Form":
                    file = File.ReadAllText($"{ Directory.GetParent(executionContext.FunctionDirectory).FullName}\\Resources\\JFKIdentificationForm.json");
                    break;
                default:
                    // Clustering Model will be placed here
                    file = File.ReadAllText("Resources/GeneralTemplate.json");
                    break;
            }

            //deserialize JSON from file  
            string Json = file;
            var searchtextlist = JsonConvert.DeserializeObject<List<TextExtract>>(Json);

            return searchtextlist;
        }

        public static List<string> ExtractKeyValuePairs(Line[] lines, string formType, ExecutionContext executionContext)
        {
            //Initialize settings
            List<TextExtract> searchKeyList = OCRHelper.RetrieveAllSearchTextKeyFields(formType, executionContext); // Retrieve all key-fields as reference
            List<Word> textvalues = new List<Word>();
            List<Line> linevalues = new List<Line>();
            List<string> result = new List<string>();
            int x_margin, y_margin, x_width, y_height;

            // Extract regions of text words
            foreach (Line sline in lines)
            {
                int[] lvalues = sline.BoundingBox;
                linevalues.Add(new Line { Text = sline.Text, BoundingBox = lvalues });

                foreach (Word sword in sline.Words)
                {
                    int[] wvalues = sword.BoundingBox;
                    textvalues.Add(new Word { Text = sword.Text, BoundingBox = wvalues });
                }
            }

            // Search Key-Value Pairs inside the documents
            if (searchKeyList.Count > 0)
            {
                foreach (TextExtract key in searchKeyList)
                {
                    var resultkeys = linevalues.Where(a => a.Text.Contains(key.Text));
                    foreach (var fieldtext in resultkeys)
                    {
                        // Assign all fields values per text
                        x_margin = key.MarginX;
                        y_margin = key.MarginY;
                        x_width = key.Width;
                        y_height = key.Height;

                        // For every value candidate set all values above
                        string txtreply = string.Join(" ",
                                                    from a in textvalues
                                                    where
                                                    (a.BoundingBox[0] >= fieldtext.BoundingBox[0] + x_margin) &&
                                                    (a.BoundingBox[0] <= fieldtext.BoundingBox[0] + x_margin + x_width) &&
                                                    (a.BoundingBox[1] >= fieldtext.BoundingBox[1] + y_margin) &&
                                                    (a.BoundingBox[1] <= fieldtext.BoundingBox[1] + y_height)
                                                    select (string)a.Text);
                        result.Add(fieldtext.Text + " - " + txtreply);
                    }
                }
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
