using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MelissaData;


namespace MelissaProfilerObjectLinuxDotnet
{
  class Program
  {
    static void Main(string[] args)
    {
      // Variables
      string license = "";
      string testFile = "";
      string dataPath = "";

      ParseArguments(ref license, ref testFile, ref dataPath, args);
      RunAsConsole(license, testFile, dataPath);
    }

    static void ParseArguments(ref string license, ref string testFile, ref string dataPath, string[] args)
    {
      for (int i = 0; i < args.Length; i++)
      {
        if (args[i].Equals("--license") || args[i].Equals("-l"))
        {
          if (args[i + 1] != null)
          {
            license = args[i + 1];
          }
        }
        if (args[i].Equals("--file") || args[i].Equals("-f"))
        {
          if (args[i + 1] != null)
          {
            testFile = args[i + 1];
          }
        }
        if (args[i].Equals("--dataPath") || args[i].Equals("-d"))
        {
          if (args[i + 1] != null)
          {
            dataPath = args[i + 1];
          }
        }
      }
    }

    static void RunAsConsole(string license, string testFile, string dataPath)
    {
      Console.WriteLine("\n\n============== WELCOME TO MELISSA PROFILER OBJECT LINUX DOTNET ==============\n");
      
      ProfilerObject profilerObject = new ProfilerObject(license, dataPath);

      bool shouldContinueRunning = true;

      if (profilerObject.mdProfilerObj.GetInitializeErrorString() != "No error.")
      {
        shouldContinueRunning = false;
      }

      while (shouldContinueRunning)
      {
        DataContainer dataContainer = new DataContainer();

        if (string.IsNullOrEmpty(testFile))
        {
          Console.WriteLine("\nFill in each value to see the Profiler Object results");

          Console.Write("File Path: ");
          dataContainer.InputFile = Console.ReadLine();
        }
        else
        {
          dataContainer.InputFile = testFile;
        }

        // Print user input
        Console.WriteLine("\n=================================== INPUTS ==================================\n");

        List<string> sections = dataContainer.GetWrapped(dataContainer.InputFile, 50);

        Console.WriteLine($"\t                Input File: {sections[0]}");

        for (int i = 1; i < sections.Count; i++)
        {
          if ((i == sections.Count - 1) && sections[i].EndsWith("/"))
          {
            sections[i] = sections[i].Substring(0, sections[i].Length - 1);
          }
          Console.WriteLine($"\t                            {sections[i]}");
        }

        // Execute Profiler Object
        profilerObject.ExecuteObjectAndResultCodes(ref dataContainer);

        // Print output
        Console.WriteLine("\n=================================== OUTPUT ==================================\n");
        Console.WriteLine("\n\tProfiler Object Information:");
        Console.WriteLine("\n\t        TABLE STATISTICS\n\n");
        Console.WriteLine("\t         TableRecordCount           :  {0}", profilerObject.mdProfilerObj.GetTableRecordCount());
        Console.WriteLine("\t         ColumnCount                :  {0}", profilerObject.mdProfilerObj.GetColumnCount());
        Console.WriteLine("");
        Console.WriteLine("\t         ExactMatchDistinctCount    :  {0}", profilerObject.mdProfilerObj.GetTableExactMatchDistinctCount());
        Console.WriteLine("\t         ExactMatchDupesCount       :  {0}", profilerObject.mdProfilerObj.GetTableExactMatchDupesCount());
        Console.WriteLine("\t         ExactMatchLargestGroup     :  {0}", profilerObject.mdProfilerObj.GetTableExactMatchLargestGroup());
        Console.WriteLine("");
        Console.WriteLine("\t         ContactMatchDistinctCount  :  {0}", profilerObject.mdProfilerObj.GetTableContactMatchDistinctCount());
        Console.WriteLine("\t         ContactMatchDupesCount     :  {0}", profilerObject.mdProfilerObj.GetTableContactMatchDupesCount());
        Console.WriteLine("\t         ContactMatchLargestGroup   :  {0}", profilerObject.mdProfilerObj.GetTableContactMatchLargestGroup());
        Console.WriteLine("");
        Console.WriteLine("\t         HouseholdMatchDistinctCount:  {0}", profilerObject.mdProfilerObj.GetTableHouseholdMatchDistinctCount());
        Console.WriteLine("\t         HouseholdMatchDupesCount   :  {0}", profilerObject.mdProfilerObj.GetTableHouseholdMatchDupesCount());
        Console.WriteLine("\t         HouseholdMatchLargestGroup :  {0}", profilerObject.mdProfilerObj.GetTableHouseholdMatchLargestGroup());
        Console.WriteLine("");
        Console.WriteLine("\t         AddressMatchDistinctCount  :  {0}", profilerObject.mdProfilerObj.GetTableAddressMatchDistinctCount());
        Console.WriteLine("\t         AddressMatchDupesCount     :  {0}", profilerObject.mdProfilerObj.GetTableAddressMatchDupesCount());
        Console.WriteLine("\t         AddressMatchLargestGroup   :  {0}", profilerObject.mdProfilerObj.GetTableAddressMatchLargestGroup());

        Console.WriteLine("\n\n\t        COLUMN STATISTICS\n\n");

        // STATE Iterator Example
        Console.WriteLine("\t         STATE Value        Count");
        profilerObject.mdProfilerObj.StartDataFrequency("state", mdProfiler.Order.OrderCountAscending);
        do
        {
          string state = profilerObject.mdProfilerObj.GetDataFrequencyValue("state").ToString();
          string count = profilerObject.mdProfilerObj.GetDataFrequencyCount("state").ToString();

          Console.WriteLine($"\t              {state,-16}{count,-10}");

        } while (profilerObject.mdProfilerObj.GetNextDataFrequency("state") == 1);
        Console.WriteLine("");


        // POSTAL Iterator Example
        Console.WriteLine("\t         POSTAL Pattern     Count");
        profilerObject.mdProfilerObj.StartPatternFrequency("zip", mdProfiler.Order.OrderCountAscending);
        do
        {
          string zipCode = profilerObject.mdProfilerObj.GetPatternFrequencyValue("zip").ToString();
          string count = profilerObject.mdProfilerObj.GetPatternFrequencyCount("zip").ToString();

          Console.WriteLine($"\t              {zipCode,-16}{count,-10}");

        } while (profilerObject.mdProfilerObj.GetNextPatternFrequency("zip") == 1);

        bool isValid = false;
        if (!string.IsNullOrEmpty(testFile))
        {
          isValid = true;
          shouldContinueRunning = false;
        }
        while (!isValid)
        {
          Console.WriteLine("\nTest another file? (Y/N)");
          string testAnotherResponse = Console.ReadLine();

          if (!string.IsNullOrEmpty(testAnotherResponse))
          {
            testAnotherResponse = testAnotherResponse.ToLower();
            if (testAnotherResponse == "y")
            {
              isValid = true;
            }
            else if (testAnotherResponse == "n")
            {
              isValid = true;
              shouldContinueRunning = false;
            }
            else
            {
              Console.Write("Invalid Response, please respond 'Y' or 'N'");
            }
          }
        }
      }
      profilerObject.mdProfilerObj.Dispose();

      Console.WriteLine("\n================== THANK YOU FOR USING MELISSA DOTNET OBJECT ================\n");
    }
  }

  class ProfilerObject
  {
    // Path to Profiler Object data files (.dat, etc)
    string dataFilePath;

    // Create instance of Melissa Profiler Object
    public mdProfiler mdProfilerObj = new mdProfiler();

    public ProfilerObject(string license, string dataPath)
    {
      // Set license string and set path to data files (.dat, etc)
      mdProfilerObj.SetLicenseString(license);
      mdProfilerObj.SetFileName("testFile.prf");
      mdProfilerObj.SetAppendMode(mdProfiler.AppendMode.Overwrite);

      dataFilePath = dataPath;
      mdProfilerObj.SetPathToProfilerDataFiles(dataFilePath);

      mdProfilerObj.SetSortAnalysis(1); // the default is 1
      mdProfilerObj.SetMatchUpAnalysis(1);// the default is 1
      mdProfilerObj.SetRightFielderAnalysis(1);// the default is 1
      mdProfilerObj.SetDataAggregation(1);// the default is 1

      // If you see a different date than expected, check your license string and either download the new data files or use the Melissa Updater program to update your data files.  
      mdProfiler.ProgramStatus pStatus = mdProfilerObj.InitializeDataFiles();

      if (pStatus != mdProfiler.ProgramStatus.ErrorNone)
      {
        Console.WriteLine("Failed to Initialize Object.");
        Console.WriteLine(pStatus);
        return;
      }

      Console.WriteLine($"                       DataBase Date: {mdProfilerObj.GetDatabaseDate()}");
      Console.WriteLine($"                     Expiration Date: {mdProfilerObj.GetLicenseExpirationDate()}");

      /**
       * This number should match with the file properties of the Melissa Object binary file.
       * If TEST appears with the build number, there may be a license key issue.
       */
      Console.WriteLine($"                      Object Version: {mdProfilerObj.GetBuildNumber()}\n");
    }

    // This will call the lookup function to process the input address as well as generate the result codes
    public void ExecuteObjectAndResultCodes(ref DataContainer data)
    {
      mdProfilerObj.AddColumn("first", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeFirstName);
      mdProfilerObj.AddColumn("last", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeLastName);
      mdProfilerObj.AddColumn("address", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeAddress);
      mdProfilerObj.AddColumn("city", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeCity);
      mdProfilerObj.AddColumn("state", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeStateOrProvince);
      mdProfilerObj.AddColumn("zip", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeZipOrPostalCode);

      string[] records = null;
      try
      {
        records = File.ReadAllLines(data.InputFile, Encoding.UTF8);
      }
      catch (Exception)
      {
        Console.WriteLine("Error: Unable to open the input file");
        Environment.Exit(1);
      }

      mdProfilerObj.StartProfiling();

      // Inputting the records to the Profiler Object
      foreach (string record in records)
      {
        string[] fields = record.Split(new char[] { ',' });
        mdProfilerObj.SetColumn("first", fields[0]);
        mdProfilerObj.SetColumn("last", fields[1]);
        mdProfilerObj.SetColumn("address", fields[2]);
        mdProfilerObj.SetColumn("city", fields[3]);
        mdProfilerObj.SetColumn("state", fields[4]);
        mdProfilerObj.SetColumn("zip", fields[5]);

        mdProfilerObj.AddRecord();
      }

      mdProfilerObj.ProfileData();

      // ResultsCodes explain any issues Profiler Object has with the object.
      // List of result codes for Profiler Object
      // https://wiki.melissadata.com/index.php?title=Result_Code_Details#Profiler_Object
    }
  }

  public class DataContainer
  {
    public string InputFile { get; set; } = "";

    public List<string> GetWrapped(string path, int maxLineLength)
    {
      FileInfo file = new FileInfo(path);
      string filePath = file.FullName;

      string[] lines = filePath.Split(new string[] { "/" }, StringSplitOptions.None);
      string currentLine = "";
      List<string> wrappedString = new List<string>();

      foreach (string section in lines)
      {
        if ((currentLine + section).Length > maxLineLength)
        {
          wrappedString.Add(currentLine.Trim());
          currentLine = "";
        }

        if (section.Contains(path))
        {
          currentLine += section;
        }
        else
        {
          currentLine += section + "/";
        }
      }

      if (currentLine.Length > 0)
      {
        wrappedString.Add(currentLine.Trim());
      }

      return wrappedString;
    }

  }
}

