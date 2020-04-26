using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ZerGo0.MinebotBlockParser
{
    internal class Program
    {
        private const string GetBlockNamesCommand = "GetBlockNames";
        private const string GenerateBlockCsFileNewCommand = "GenerateBlockCsFileNew";
        private const string GenerateBlockCsFileOldCommand = "GenerateBlockCsFileOld";

        private static readonly string OutputPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Output\\";


        private static void Main(string[] args)
        {
            switch (args[0])
            {
                default:
                    Console.WriteLine("Command not regonized! This are the commands:");
                    Console.WriteLine("");
                    Console.WriteLine("GetBlockNames \"C:\\Path\\To\\blocksXXX.json\"");
                    Console.WriteLine("GenerateBlockCsFileNew \"C:\\Path\\To\\blocksXXX.json\"");
                    Console.WriteLine("GenerateBlockCsFileOld \"C:\\Path\\To\\blocksXXX.json\"");
                    Console.WriteLine("");
                    Console.WriteLine("Exiting!");
                    break;
                case GetBlockNamesCommand:
                    GetBlockNames(args[1]);
                    break;
                case GenerateBlockCsFileNewCommand:
                    GenerateBlockCsNewFile(args[1]);
                    break;
                case GenerateBlockCsFileOldCommand:
                    GenerateBlockCsOldFile(args[1]);
                    break;
            }
        }

        private static void GetBlockNames(string blocksJsonPath)
        {
            Console.WriteLine("Scrapping Blocknames...");

            var blockJsonJObject = JObject.Parse(File.ReadAllText(blocksJsonPath));
            var tempBlockList = blockJsonJObject.Properties()
                .Select(item => string.Concat(item.Name.Replace("minecraft:", "")
                    .Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))))
                .ToList();

            Console.WriteLine($"{tempBlockList.Count} Blocksnames scrapped");

            if (!Directory.Exists(OutputPath))
                try
                {
                    Console.WriteLine("Output directory doesn't exist, trying to create it...");
                    Directory.CreateDirectory(OutputPath);
                    Console.WriteLine("Output directory created");
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured while trying to create the output directory, exiting!");
                    throw;
                }

            var outputFile = OutputPath + "BlockNames.txt";

            try
            {
                Console.WriteLine("Trying to save the output file...");
                if (File.Exists(outputFile)) File.Delete(outputFile);

                using (var fs = File.Create(outputFile))
                {
                    foreach (var tempBlock in tempBlockList.Select(
                        block => "public static Block " + block + ";\n"))
                        fs.Write(new UTF8Encoding(true).GetBytes(tempBlock), 0, tempBlock.Length);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("An error occured while trying to write to the output file, exiting!");
                throw;
            }

            Console.WriteLine($"Output file successfully saved! {outputFile}");
        }

        private static void GenerateBlockCsNewFile(string blocksJsonPath)
        {
            //blocksXXX.json > BlocksXXX.cs
            var tempFilename = Path.GetFileNameWithoutExtension(blocksJsonPath);
            tempFilename = char.ToUpper(tempFilename[0]) + tempFilename.Substring(1);

            Console.WriteLine("Scrapping Blockdata...");

            var blockDictonary = new Dictionary<string, string>();
            var blocksJsonJObject = JObject.Parse(File.ReadAllText(blocksJsonPath));
            foreach (var block in blocksJsonJObject.Properties())
            {
                var blockName = string.Concat(block.Name.Replace("minecraft:", "")
                    .Split('_').Select(word => char.ToUpper(word[0]) + word.Substring(1)));

                if (block.Value.SelectToken("states") == null) continue;

                var blockIds = (block.Value.SelectToken("states") ??
                                throw new InvalidOperationException(
                                    "States in block.json file not found, right format?"))
                    .Select(state => state.SelectToken("id")).Select(blockId =>
                        blockId != null ? Convert.ToUInt16(blockId.ToString()) : (ushort) 0)
                    .ToList();

                var blockIdsString = "";
                var firstBlockId = false;
                foreach (var blockId in blockIds)
                    if (!firstBlockId)
                    {
                        firstBlockId = true;
                        blockIdsString += blockId;
                    }
                    else
                    {
                        blockIdsString += $", {blockId}";
                    }

                blockDictonary.Add(blockName, blockIdsString);
            }

            Console.WriteLine($"Scrapped data from {blockDictonary.Count} blocks");

            var outputFileContent = new List<string>();
            outputFileContent.AddRange(new[]
            {
                "namespace MinebotBlockClasses",
                "{",
                "    public class " + tempFilename,
                "    {",
                "        public static void SetBlocks()",
                "        {"
            });

            outputFileContent.AddRange(blockDictonary.Select(block =>
                "            Blocks." + block.Key + " = new Block(new ushort[]{" + block.Value + "}, new ushort[]{0});"));

            outputFileContent.AddRange(new[]
            {
                "        }",
                "    }",
                "}"
            });


            var outputFile = OutputPath + tempFilename + ".cs";
            if (!Directory.Exists(OutputPath))
                try
                {
                    Console.WriteLine("Output directory doesn't exist, trying to create it...");
                    Directory.CreateDirectory(OutputPath);
                    Console.WriteLine("Output directory created");
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured while trying to create the output directory, exiting!");
                    throw;
                }

            try
            {
                Console.WriteLine("Trying to save the output file...");
                if (File.Exists(OutputPath + tempFilename)) File.Delete(OutputPath + tempFilename);

                File.WriteAllLines(outputFile, outputFileContent);
            }
            catch (Exception)
            {
                Console.WriteLine("An error occured while trying to write to the output file, exiting!");
                throw;
            }

            Console.WriteLine($"Output file successfully saved! {outputFile}");
        }

        private static void GenerateBlockCsOldFile(string blocksJsonPath)
        {
            //blocksXXX.json > BlocksXXX.cs
            var tempFilename = Path.GetFileNameWithoutExtension(blocksJsonPath);
            tempFilename = char.ToUpper(tempFilename[0]) + tempFilename.Substring(1);

            Console.WriteLine("Scrapping Blockdata...");

            var blockDictonary = new Dictionary<string, string>();
            var blocksJsonJArray = JArray.Parse(File.ReadAllText(blocksJsonPath));
            foreach (var blockJsonJArray in blocksJsonJArray)
            {
                var blockJsonJObject = JObject.Parse(blockJsonJArray.ToString());

                var blockName = string.Concat(blockJsonJObject["displayName"]
                    ?.ToString()
                    .Split(' ').Select(word => char.ToUpper(word[0]) + word.Substring(1)) ?? throw new
                    InvalidOperationException("\"displayName\" null"));
                var blockId = blockJsonJObject["id"]?.ToString();
                var blockMetadatas = new List<ushort>();

                if (blockJsonJObject["variations"] != null)
                {
                    var variations = JArray.Parse(blockJsonJObject["variations"].ToString());
                    blockMetadatas.AddRange(variations.Select(variation =>
                        Convert.ToUInt16(variation?["metadata"]?.ToString())));

                    var blockIdsString = "";
                    var firstBlockMetadata = false;
                    foreach (var blockMetadata in blockMetadatas)
                        if (!firstBlockMetadata)
                        {
                            firstBlockMetadata = true;
                            blockIdsString += blockId + ":" + blockMetadata;
                        }
                        else
                        {
                            blockIdsString += $", {blockMetadata}";
                        }

                    Console.WriteLine($"{blockName} | {blockIdsString}");
                    blockDictonary.Add(blockName, blockIdsString);
                }
                else
                {
                    var blockIdsString = blockId + ":" + 0;
                    Console.WriteLine($"{blockName} | {blockIdsString}");
                    blockDictonary.Add(blockName, blockIdsString);
                }
            }

            Console.WriteLine($"Scrapped data from {blockDictonary.Count} blocks");

            var outputFileContent = new List<string>();
            outputFileContent.AddRange(new[]
            {
                "namespace MinebotBlockClasses",
                "{",
                "    public class " + tempFilename,
                "    {",
                "        public static void SetBlocks()",
                "        {"
            });

            outputFileContent.AddRange(blockDictonary.Select(block =>
                "            Blocks." + block.Key + " = new Block(new ushort[]{" + block.Value.Split(':')[0] +
                "}, new ushort[]{" +
                block.Value.Split(':')[1] + "});"));

            outputFileContent.AddRange(new[]
            {
                "        }",
                "    }",
                "}"
            });


            var outputFile = OutputPath + tempFilename + ".cs";
            if (!Directory.Exists(OutputPath))
                try
                {
                    Console.WriteLine("Output directory doesn't exist, trying to create it...");
                    Directory.CreateDirectory(OutputPath);
                    Console.WriteLine("Output directory created");
                }
                catch (Exception)
                {
                    Console.WriteLine("An error occured while trying to create the output directory, exiting!");
                    throw;
                }

            try
            {
                Console.WriteLine("Trying to save the output file...");
                if (File.Exists(OutputPath + tempFilename)) File.Delete(OutputPath + tempFilename);

                File.WriteAllLines(outputFile, outputFileContent);
            }
            catch (Exception)
            {
                Console.WriteLine("An error occured while trying to write to the output file, exiting!");
                throw;
            }

            Console.WriteLine($"Output file successfully saved! {outputFile}");
        }
    }
}