#!/bin/bash

# Name:    MelissaProfilerObjectLinuxDotnet
# Purpose: Use the MelissaUpdater to make the MelissaProfilerObjectLinuxDotnet code usable

######################### Constants ##########################

RED='\033[0;31m' #RED
NC='\033[0m' # No Color

######################### Parameters ##########################

file=""
license=""
quiet="false"

while [ $# -gt 0 ] ; do
  case $1 in
    --file) 
        file="$2"

        if [ "$file" == "--license" ] || [ "$file" == "--quiet" ] || [ -z "$file" ];
        then
            printf "${RED}Error: Missing an argument for parameter \'file\'.${NC}\n"  
            exit 1
        fi  
        ;;
    --license) 
        license="$2"

        if [ "$license" == "--file" ] || [ "$license" == "--quiet" ] || [ -z "$license" ];
        then
            printf "${RED}Error: Missing an argument for parameter \'license\'.${NC}\n"  
            exit 1
        fi    
        ;;
    --quiet) 
        quiet="true" 
        
        if [ "$quiet" == "--file" ] || [ "$quiet" == "--license" ] || [ -z "$quiet" ];
        then
            printf "${RED}Error: problem setting \'quiet\'.${NC}\n"  
            exit 1
        fi    
        ;;
  esac
  shift
done

######################### Config ###########################

RELEASE_VERSION='2023.12'
ProductName="profiler_data"

# Uses the location of the .sh file 
# Modify this if you want to use 
CurrentPath=$(pwd)
ProjectPath="$CurrentPath/MelissaProfilerObjectLinuxDotnet"
BuildPath="$ProjectPath/Build"
DataPath="$ProjectPath/Data"

if [ ! -d $DataPath ];
then
    mkdir $DataPath
fi

if [ ! -d $BuildPath ];
then
    mkdir $BuildPath
fi

# Config variables for download file(s)
Config1_FileName="libmdProfiler.so"
Config1_ReleaseVersion=$RELEASE_VERSION
Config1_OS="LINUX"
Config1_Compiler="GCC41"
Config1_Architecture="64BIT"
Config1_Type="BINARY"

Wrapper_FileName="mdProfiler_cSharpCode.cs"
Wrapper_ReleaseVersion=$RELEASE_VERSION
Wrapper_OS="ANY"
Wrapper_Compiler="NET"
Wrapper_Architecture="ANY"
Wrapper_Type="INTERFACE"

# ######################## Functions #########################

DownloadDataFiles()
{
    printf "\n=============================== MELISSA UPDATER =============================\n"
    printf "MELISSA UPDATER IS DOWNLOADING DATA FILE(S)...\n"

    ./MelissaUpdater/MelissaUpdater manifest -p $ProductName -r $RELEASE_VERSION -l $1 -t $DataPath 
    if [ $? -ne 0 ];
    then
        printf "\nCannot run Melissa Updater. Please check your license string!\n"
        exit 1
    fi     

    printf "Melissa Updater finished downloading data file(s)!\n"
}

DownloadSO() 
{
    printf "\nMELISSA UPDATER IS DOWNLOADING SO(S)...\n"
    
    # Check for quiet mode
    if [ $quiet == "true" ];
    then
        ./MelissaUpdater/MelissaUpdater file --filename $Config1_FileName --release_version $Config1_ReleaseVersion --license $1 --os $Config1_OS --compiler $Config1_Compiler --architecture $Config1_Architecture --type $Config1_Type --target_directory $BuildPath &> /dev/null
        if [ $? -ne 0 ];
        then
            printf "\nCannot run Melissa Updater. Please check your license string!\n"
            exit 1
        fi

        printf "Melissa Updater finished downloading $Config1_FileName!\n"

    else
        ./MelissaUpdater/MelissaUpdater file --filename $Config1_FileName --release_version $Config1_ReleaseVersion --license $1 --os $Config1_OS --compiler $Config1_Compiler --architecture $Config1_Architecture --type $Config1_Type --target_directory $BuildPath 
        if [ $? -ne 0 ];
        then
            printf "\nCannot run Melissa Updater. Please check your license string!\n"
            exit 1
        fi

        printf "Melissa Updater finished downloading $Config1_FileName!\n"
    fi
}

DownloadWrapper() 
{
    printf "\nMELISSA UPDATER IS DOWNLOADING WRAPPER(S)...\n"
    
    # Check for quiet mode
    if [ $quiet == "true" ];
    then
        ./MelissaUpdater/MelissaUpdater file --filename $Wrapper_FileName --release_version $Wrapper_ReleaseVersion --license $1 --os $Wrapper_OS --compiler $Wrapper_Compiler --architecture $Wrapper_Architecture --type $Wrapper_Type --target_directory $ProjectPath &> /dev/null
        if [ $? -ne 0 ];
        then
            printf "\nCannot run Melissa Updater. Please check your license string!\n"
            exit 1
        fi
    else
        ./MelissaUpdater/MelissaUpdater file --filename $Wrapper_FileName --release_version $Wrapper_ReleaseVersion --license $1 --os $Wrapper_OS --compiler $Wrapper_Compiler --architecture $Wrapper_Architecture --type $Wrapper_Type --target_directory $ProjectPath 
        if [ $? -ne 0 ];
        then
            printf "\nCannot run Melissa Updater. Please check your license string!\n"
            exit 1
        fi
    fi
    
    printf "Melissa Updater finished downloading $Wrapper_FileName!\n"
}

CheckSOs() 
{
    if [ ! -f $BuildPath/$Config1_FileName ];
    then
        echo "false"
    else
        echo "true"
    fi
}

########################## Main ############################

printf "\n========================== Melissa Profiler Object ==========================\n                          [ .NET | Linux | 64BIT ]\n"

# Get license (either from parameters or user input)
if [ -z "$license" ];
then
  printf "Please enter your license string: "
  read license
fi

# Check for License from Environment Variables 
if [ -z "$license" ];
then
  license=`echo $MD_LICENSE` 
fi

if [ -z "$license" ];
then
  printf "\nLicense String is invalid!\n"
  exit 1
fi

# Use Melissa Updater to download data file(s) 
# Download data file(s) 
DownloadDataFiles $license      # comment out this line if using DQS Release

# Set data file(s) path
#DataPath=""      # uncomment this line and change to your DQS Release data file(s) directory 

#if [ ! -d $DataPath ]; # uncomment this section of code if you are using your own DQS Release data file(s) directory
#then
    #printf "\nData path is invalid!\n"
    #exit 1
#fi

# Download SO(s)
DownloadSO $license 

# Download wrapper(s)
DownloadWrapper $license

# Check if all SO(s) have been downloaded. Exit script if missing
printf "\nDouble checking SO file(s) were downloaded...\n"

SOsAreDownloaded=$(CheckSOs)

if [ "$SOsAreDownloaded" == "false" ];
then
    printf "\nMissing data file(s).  Please check that your license string and directory are correct.\n"

    printf "\nAborting program, see above.\n"
    exit 1
fi

printf "\nAll file(s) have been downloaded/updated!\n"

# Start program
# Build project
printf "\n================================ BUILD PROJECT ==============================\n"

dotnet publish -f="net8.0" -c Release -o $BuildPath MelissaProfilerObjectLinuxDotnet/MelissaProfilerObjectLinuxDotnet.csproj

# Run Project
if [ -z "$file" ];
then
    pushd MelissaProfilerObjectLinuxDotnet
    dotnet $BuildPath/MelissaProfilerObjectLinuxDotnet.dll --license $license --dataPath $DataPath
    popd
else
    pushd MelissaProfilerObjectLinuxDotnet
    dotnet $BuildPath/MelissaProfilerObjectLinuxDotnet.dll --license $license --dataPath $DataPath --file "$file"
    popd
fi
