using System;
using System.ComponentModel;
using CsvQuery.PluginInfrastructure; // for SettingsBase
using ITS.Utils;
using static ITS.Utils.ITSENums;


namespace JSON_Tools.Utils
{
    /// <summary>
    /// Manages application settings
    /// </summary>
    public class Settings : SettingsBase
    {
        #region 1-General
        [Description("Initials used for creating a change\r\n" +
                     "comment.  Change comments go in columns 1-6\r\n" +
                     "and are formatted as xxmmyy Where:\r\n" +
                     "xx - Initials entered here \r\n" +
                     "mm - current numeric month of year\r\n" +
                     "yy - last two digits of current year."
            ),
            Category("1-General"), DefaultValue("??")]
        public string initials { get; set; }

        [Description("Change comment format. Change comments go in columns 1-6\r\n" +
                     "Formats are:\r\n" +
                     "iimmyy - 2 initials followed by the numeric month and year.\r\n" +
                     "mmyyii - Numeric month and year followed by 2 initials.\r\n"),
            Category("1-General"), DefaultValue(ITSENums.CHG_CMMT_FORMAT.IIMMYY)]
        public CHG_CMMT_FORMAT chgFormat { get; set; }

        [Description("Working Environment.  Used for file/proc/record retrieval."),
            Category("1-General"), DefaultValue(ITSENums.ENVIRONMENT.Development)]
        public ENVIRONMENT workingEnvt { get; set; }

        [Description("Drive letter mapped to OS2200 (root)"),
            Category("1-General"), DefaultValue(ITSENums.MAPPED_DRIVE.A)]
        public MAPPED_DRIVE mappedDrive { get; set; }

        [Description("Workspace Source File name (qual*file)"),
            Category("1-General"), DefaultValue("")]
        public string WorkSpaceSRCFile { get; set; }

        [Description("System Proc File name (qual*file)" +
                     "This is the file where DPS system procs arae defined."),
            Category("1-General"), DefaultValue("")]
        public string systemProcFile { get; set; }

        [Description("Windows path to a scratch (work) folder.\r\n" +
                     "This is the folder where files with expanded copy\r\n" + 
                     "procs are saved. Example O:\\NPP_Scratch_Folder\\"),
            Category("1-General"), DefaultValue("")]
        public string scratchFolderPath { get; set; }

        #endregion

        #region 2-Development Environment
        [Description("Source file name (qual*file)"),
            Category("2-Development Environment"), DefaultValue("")]
        public string DEVSRCFile { get; set; }

        [Description("UCOB   - Proc file name (qual*file)"),
            Category("2-Development Environment"), DefaultValue("")]
        public string DEVUCOBprocFile { get; set; }

        [Description("ACOB Proc 1 - Proc file name (qual*file)"),
            Category("2-Development Environment"), DefaultValue("")]
        public string DEVACOBproc1File { get; set; }

        [Description("ACOB Proc 2 - Proc file name (qual*file)"),
            Category("2-Development Environment"), DefaultValue("")]
        public string DEVACOBproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]) - Example, \r\n" +
                     "qqq*fff.S$PROC/version"),
            Category("2-Development Environment"), DefaultValue("")]
        public string DEVschemaFile { get; set; }

        #endregion
        #region 3-User Test Environment
        [Description("Source file name (qual*file) - Example, \r\n" +
                     "IM5*LDMIPSRC"),
            Category("3-User Test Environment"), DefaultValue("")]
        public string TSTSRCFile { get; set; }

        [Description("UCOB   - Proc file name (qual*file) - Example, \r\n" +
                     "UIM5*LDMIPCOPY"),
            Category("3-User Test Environment"), DefaultValue("")]
        public string TSTUCOBprocFile { get; set; }

        [Description("ACOPB Proc 1 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY"),
            Category("3-User Test Environment"), DefaultValue("")]
        public string TSTACOBproc1File { get; set; }

        [Description("ACOB Proc 2 - Proc file name (qual*file)"),
            Category("3-User Test Environment"), DefaultValue("")]
        public string TSTACOBproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]), Example: \r\n" +
                     "qqq*fff.S$PROC/version"),
            Category("3-User Test Environment"), DefaultValue("")]
        public string TSTschemaFile { get; set; }
        #endregion

        #region 4-Pseudo Environment
        [Description("Source file name (qual*file)"),
            Category("4-Pseudo Environment"), DefaultValue("")]
        public string PSDSRCFile { get; set; }

        [Description("UCOB - Proc file name (qual*file)"),
            Category("4-Pseudo Environment"), DefaultValue("")]
        public string PSDUCOBprocFile { get; set; }

        [Description("Proc 1 - Proc file name (qual*file)"),
            Category("4-Pseudo Environment"), DefaultValue("")]
        public string PSDACOBproc1File { get; set; }

        [Description("Proc 2 - Proc file name (qual*file) - Example"),
            Category("4-Pseudo Environment"), DefaultValue("")]
        public string PSDACOBproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]), Example: \r\n" +
                     "qqq*fff.S$PROC/version"),
            Category("4-Pseudo Environment"), DefaultValue("")]
        public string PSDschemaFile { get; set; }

        #endregion


    }
}

