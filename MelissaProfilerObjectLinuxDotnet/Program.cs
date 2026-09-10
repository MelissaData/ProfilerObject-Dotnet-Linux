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
  /// <summary>
  /// The Profiler Object is a data quality tool that provides statistical analysis
  /// and assessment of your data quality needs for consistency, uniqueness and correctness.
  /// </summary>
  /// <remarks>
  /// High-level flow of this sample:
  ///   1. SETUP     - create an mdProfiler instance, license it, configure the analyses and
  ///                  output file, point it at the data files, then InitializeDataFiles().
  ///   2. INPUT     - define the columns (AddColumn), then read a CSV file and feed each
  ///                  row in with SetColumn + AddRecord.
  ///   3. PROCESS   - ProfileData() analyzes the whole table.
  ///   4. READ      - pull table-level statistics with the GetTable* getters, and per-column
  ///                  value/pattern distributions with the frequency iterators.
  ///   5. CLEANUP   - Dispose() releases the object when finished.
  ///
  /// The pieces in this file map onto that flow:
  ///   - Program        : console harness (argument parsing + the interactive loop).
  ///   - ProfilerObject : thin wrapper around mdProfiler (setup + the profiling sequence).
  ///   - DataContainer  : holds the input file path and a small path-wrapping helper.
  ///
  /// Where mdProfiler comes from:
  ///   The MelissaData namespace and its mdProfiler class live in mdProfiler_cSharpCode.cs,
  ///   a generated C# wrapper over libmdProfiler.so that the accompanying
  ///   MelissaProfilerObjectLinuxDotnet.sh script downloads on every run.
  ///
  /// Reference:
  ///   Quickstart    : https://docs.melissa.com/on-premise-api/profiler-object/profiler-object-quickstart.html
  ///   Release notes : https://releasenotes.melissa.com/on-premise-api/profiler-object/
  ///   Result codes  : https://docs.melissa.com/on-premise-api/profiler-object/result-codes.html
  /// </remarks>
  class Program
  {
    /// <summary>
    /// Entry point. Reads the optional command-line arguments, then hands control to
    /// RunAsConsole, which performs the actual Profiler Object setup and processing.
    /// </summary>
    /// <param name="args">The raw command-line arguments</param>
    static void Main(string[] args)
    {
      // Populated by ParseArguments below.
      string license = "";
      string testFile = "";
      string dataPath = "";

      ParseArguments(ref license, ref testFile, ref dataPath, args);
      RunAsConsole(license, testFile, dataPath);
    }

    /// <summary>
    /// Reads the supported command-line options into the ref parameters.
    ///
    /// Recognized flags (each followed by its value):
    ///   --license / -l   : the Melissa license string
    ///   --file / -f      : path to the CSV file to profile in one-shot mode
    ///   --dataPath / -d  : path to the Profiler Object data files
    /// </summary>
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

    /// <summary>
    /// Sets up the Profiler Object once, then drives the input -> process -> output cycle.
    ///
    /// In interactive mode (no --file) it loops, asking for a file path each pass until the
    /// user answers "N". In one-shot mode (--file supplied) it runs a single pass and exits.
    /// </summary>
    static void RunAsConsole(string license, string testFile, string dataPath)
    {
      Console.WriteLine("\n\n============== WELCOME TO MELISSA PROFILER OBJECT LINUX DOTNET ==============\n");
      
      // Construct the wrapper. This is where the object is licensed, configured, pointed
      // at the data files, and initialized (see the ProfilerObject constructor below).
      ProfilerObject profilerObject = new ProfilerObject(license, dataPath);

      bool shouldContinueRunning = true;

      // Gate the program on a successful initialization. If the data files could not
      // be loaded (bad/expired license, missing or wrong-path data files, ...),
      // GetInitializeErrorString() returns the reason instead of "No error." and we
      // skip the processing loop entirely.
      if (profilerObject.mdProfilerObj.GetInitializeErrorString() != "No error.")
      {
        shouldContinueRunning = false;
      }

      while (shouldContinueRunning)
      {
        // Holder for this pass's input file path.
        DataContainer dataContainer = new DataContainer();

        if (string.IsNullOrEmpty(testFile))
        {
          // Interactive mode: prompt the user for a file path.
          Console.WriteLine("\nFill in each value to see the Profiler Object results");

          Console.Write("File Path: ");
          dataContainer.InputFile = Console.ReadLine();
        }
        else
        {
          // One-shot mode: use the file path passed on the command line.
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
        // Profiles the input file; the statistics are then read from the object below.
        profilerObject.ExecuteObjectAndResultCodes(ref dataContainer);

        // Print output
        // The output has two parts: table-level statistics (record/column counts and
        // duplicate analysis) read via GetTable* getters, then per-column frequency
        // distributions read with the iterator pattern further below.
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

        // STATE Iterator Example - walk the value-frequency list for the "state" column:
        // StartDataFrequency opens the iterator, GetDataFrequencyValue/Count read the current
        // entry, and GetNextDataFrequency advances until it returns something other than 1.
        Console.WriteLine("\t         STATE Value        Count");
        profilerObject.mdProfilerObj.StartDataFrequency("state", mdProfiler.Order.OrderCountAscending);
        do
        {
          string state = profilerObject.mdProfilerObj.GetDataFrequencyValue("state").ToString();
          string count = profilerObject.mdProfilerObj.GetDataFrequencyCount("state").ToString();

          Console.WriteLine($"\t              {state,-16}{count,-10}");

        } while (profilerObject.mdProfilerObj.GetNextDataFrequency("state") == 1);
        Console.WriteLine("");


        // POSTAL Iterator Example - same pattern as above, but StartPatternFrequency
        // reports the frequency of value *patterns* (e.g. ZIP formats) for the "zip" column.
        Console.WriteLine("\t         POSTAL Pattern     Count");
        profilerObject.mdProfilerObj.StartPatternFrequency("zip", mdProfiler.Order.OrderCountAscending);
        do
        {
          string zipCode = profilerObject.mdProfilerObj.GetPatternFrequencyValue("zip").ToString();
          string count = profilerObject.mdProfilerObj.GetPatternFrequencyCount("zip").ToString();

          Console.WriteLine($"\t              {zipCode,-16}{count,-10}");

        } while (profilerObject.mdProfilerObj.GetNextPatternFrequency("zip") == 1);

        bool isValid = false;

        // In one-shot mode there is nothing more to do after a single pass: mark the
        // input handled and stop the outer loop.
        if (!string.IsNullOrEmpty(testFile))
        {
          isValid = true;
          shouldContinueRunning = false;
        }

        // Interactive mode: ask whether to process another file. Keep prompting until
        // we get a valid Y/N. "N" ends the program; "Y" falls through to another pass.
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
      // Release the Profiler Object's resources now that we are done with it.
      profilerObject.mdProfilerObj.Dispose();

      Console.WriteLine("\n================== THANK YOU FOR USING MELISSA DOTNET OBJECT ================\n");
    }
  }

  /// <summary>
  /// Wrapper that owns a single Melissa Profiler Object instance and encapsulates the two
  /// things every Melissa object needs: one-time setup (license + config + data files) and
  /// the per-file profiling sequence. Reuse one instance; do NOT re-initialize per file.
  /// </summary>
  class ProfilerObject
  {
    // Path to the Profiler Object data files.
    string dataFilePath;

    // The underlying Melissa Profiler Object instance.
    public mdProfiler mdProfilerObj = new mdProfiler();

    /// <summary>
    /// Performs the mandatory one-time setup, in this required order:
    ///   1. SetLicenseString               - authorize the object.
    ///   2. SetFileName / SetAppendMode    - choose the profiler output file and write mode.
    ///   3. SetPathToProfilerDataFiles     - tell it where the data files live.
    ///   4. Set*Analysis / SetDataAggregation - choose which analyses to run (1 = on).
    ///   5. InitializeDataFiles            - load the data into memory.
    /// </summary>
    /// <param name="license">The Melissa license string used to authorize the object.</param>
    /// <param name="dataPath">Path to the folder containing the Profiler Object data files.</param>
    public ProfilerObject(string license, string dataPath)
    {
      // Set license string and set path to data files
      mdProfilerObj.SetLicenseString(license);
      mdProfilerObj.SetFileName("testFile.prf");
      mdProfilerObj.SetAppendMode(mdProfiler.AppendMode.Overwrite);

      dataFilePath = dataPath;
      mdProfilerObj.SetPathToProfilerDataFiles(dataFilePath);

      // The default is 1
      mdProfilerObj.SetSortAnalysis(1);
      mdProfilerObj.SetMatchUpAnalysis(1);// the default is 1
      mdProfilerObj.SetRightFielderAnalysis(1);// the default is 1
      mdProfilerObj.SetDataAggregation(1);// the default is 1

      // Load the data files. The returned ProgramStatus reports whether initialization succeeded.
      // If you see a different date than expected, check your license string and either download the new data files
      // or use the Melissa Updater program to update your data files.
      mdProfiler.ProgramStatus pStatus = mdProfilerObj.InitializeDataFiles();

      // If an issue occurred, please investigate the common causes.
      // Common causes: an invalid/expired license, or missing/wrong-path data files.
      if (pStatus != mdProfiler.ProgramStatus.ErrorNone)
      {
        Console.WriteLine("Failed to Initialize Object.");
        Console.WriteLine(pStatus);
        return;
      }

      // Diagnostic information, handy for confirming the object loaded the data you expect:

      // Build date of the data files
      Console.WriteLine($"                       DataBase Date: {mdProfilerObj.GetDatabaseDate()}");

      // When the license stops working
      Console.WriteLine($"                     Expiration Date: {mdProfilerObj.GetLicenseExpirationDate()}");

      // This number should match with the file properties of the Melissa Object binary file.
      // If TEST appears with the build number, there may be a license key issue.
      Console.WriteLine($"                      Object Version: {mdProfilerObj.GetBuildNumber()}\n");
    }

    /// <summary>
    /// Runs the full Profiler analysis for one input file. This is the canonical call
    /// pattern to copy into your own application:
    ///   AddColumn (define the schema) -> StartProfiling -> per row SetColumn + AddRecord
    ///   -> ProfileData (analyze the table).
    /// </summary>
    /// <param name="data">The record holder; its InputFile is the CSV file to profile.</param>
    public void ExecuteObjectAndResultCodes(ref DataContainer data)
    {
      // Define the table schema: one AddColumn per column, naming it and declaring the kind
      // of data it holds so the object knows how to profile each one.
      mdProfilerObj.AddColumn("first", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeFirstName);
      mdProfilerObj.AddColumn("last", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeLastName);
      mdProfilerObj.AddColumn("address", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeAddress);
      mdProfilerObj.AddColumn("city", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeCity);
      mdProfilerObj.AddColumn("state", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeStateOrProvince);
      mdProfilerObj.AddColumn("zip", mdProfiler.ProfilerColumnType.ColumnTypeVariableUnicodeString, mdProfiler.ProfilerDataType.DataTypeZipOrPostalCode);

      // Read the whole CSV input file into memory (one string per line).
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

      // Begin a profiling session, then feed every row in below.
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

      // All rows added; analyze the full table.
      mdProfilerObj.ProfileData();

      // ResultsCodes explain any issues Profiler Object has with the object.
      // List of result codes for Profiler Object
      // https://docs.melissa.com/on-premise-api/profiler-object/result-codes.html
    }
  }

  /// <summary>
  /// Holds the input file path for one pass, plus a small display helper (GetWrapped wraps
  /// a long path so it prints neatly across several lines).
  /// </summary>
  public class DataContainer
  {
    // Input: path to the CSV file to profile.
    public string InputFile { get; set; } = "";

    /// <summary>
    /// Splits a long file path into chunks no longer than maxLineLength so it prints
    /// neatly across several lines in the console output. Display-only helper.
    /// </summary>
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

