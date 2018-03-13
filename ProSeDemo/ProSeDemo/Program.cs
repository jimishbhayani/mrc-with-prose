using Microsoft.ProgramSynthesis.DslLibrary;
using Microsoft.ProgramSynthesis.Extraction.Text;
using Microsoft.ProgramSynthesis.Extraction.Text.Constraints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProSeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> paths = Directory.EnumerateFiles("examples").ToList();
            List<Dictionary<string, string>> regionsToLearn = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string> (){
                    { "First Name", "Shekhar" },
                    { "Last Name", "Malik" },
                    { "Father's Name", "Sundar Malik" },
                    { "Site Area", "97" },
                    { "Total Floor Area", "1358" },
                    { "No. of Floors", "14" },
                },
                new Dictionary<string, string> (){
                    { "First Name", "Ram" },
                    { "Last Name", "Vashistha" },
                    { "Father's Name", "Manohar Vashistha" },
                    { "Site Area", "86" },
                    { "Total Floor Area", "1032" },
                    { "No. of Floors", "12" },
                }
            };
            LearnMultipleRegionsUsingMultipleFiles(paths, regionsToLearn);
        }

        /// <summary>
        ///     Learns a program to extract a multiple regions using two examples in two different files.
        ///     Learning multiple regions is similar to learning single regions, it's just doing same task again for different regions.
        /// </summary>
        private static void LearnMultipleRegionsUsingMultipleFiles(List<string> paths, List<Dictionary<string, string>> regionsToLearn)
        {
            List<StringRegion> inputs = new List<StringRegion>();

            for (int i = 0; i < paths.Count; i++)
            {
                string s = File.ReadAllText(paths[i]);
                inputs.Add(RegionSession.CreateStringRegion(s));
            }

            int trainingDocumentCount = 2;

            List<string> fieldsToLearn = regionsToLearn[0].Keys.ToList();
            Dictionary<string, RegionSession> sessionPerField = new Dictionary<string, RegionSession>();

            foreach (string field in fieldsToLearn)
            {
                RegionSession session = new RegionSession();
                for (int i = 0; i < trainingDocumentCount; i++)
                {
                    string output = regionsToLearn[i][field];
                    uint start = inputs[i].IndexOfRelative(output).Value;
                    uint end = (uint)(start + output.Length);

                    RegionExample example = new RegionExample(inputs[i], inputs[i].Slice(start, end));
                    session.AddConstraints(example);
                }
                sessionPerField.Add(field, session);
            }
            Dictionary<string, RegionProgram> programPerField = new Dictionary<string, RegionProgram>();
            foreach(var fieldSessionPair in sessionPerField)
            {
                RegionProgram program = fieldSessionPair.Value.Learn();
                if (program == null)
                {
                    Console.Error.WriteLine("Error: Learning fails for Field : " + fieldSessionPair.Key);
                }
                else
                {
                    programPerField.Add(fieldSessionPair.Key, program);
                }
            }

            //testing
            
            StreamWriter outputWriter = new StreamWriter(@"..\..\output.txt");
            outputWriter.WriteLine(string.Join("\t|\t", programPerField.Keys));
            for (int i = trainingDocumentCount; i< inputs.Count; i++)
            {
                List<string> values = new List<string>();
                foreach (var fieldProgramPair in programPerField)
                {
                    string value = fieldProgramPair.Value.Run(inputs[i])?.Value;
                    values.Add(value);
                }
                outputWriter.WriteLine(string.Join("\t|\t\t", values));
            }
            outputWriter.Flush();
            outputWriter.Close();
        }
    }
}
