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
        #region General
        [Description("Initials used for creating a change\r\n" +
                     "comment.  Change comments go in columns 1-6\r\n" +
                     "and are formatted as xxmmyy Where:\r\n" +
                     "xx - Initials entered here \r\n" +
                     "mm - current numeric month of year\r\n" +
                     "yy - last two digits of current year."
            ),
            Category("General"), DefaultValue("??")]
        public string initials { get; set; }

        [Description("Working Environment. Used for proc/record retrieval."),
            Category("General"), DefaultValue(ITSENums.ENVIRONMENT.Development)]
        public ENVIRONMENT workingEnvt { get; set; }

        [Description("Drive letter mapped to OS2200 (root)"),
            Category("General"), DefaultValue(ITSENums.MAPPED_DRIVE.A)]
        public MAPPED_DRIVE mappedDrive { get; set; }

        [Description("Workspace Source File name (qual*file) - Example, \r\n" +
                     "IM5*GG9940SRC"),
            Category("General"), DefaultValue("")]
        public string WorkSpaceSRCFile { get; set; }

        #endregion

        #region Development Environment
        [Description("Source file name (qual*file) - Example, \r\n" +
                     "IM5*MIPSRC"),
            Category("Development Environment"), DefaultValue("")]
        public string DEVSRCFile { get; set; }

        [Description("Proc 1 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY"),
            Category("Development Environment"), DefaultValue("")]
        public string DEVproc1File { get; set; }

        [Description("Proc 2 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY2"),
            Category("Development Environment"), DefaultValue("")]
        public string DEVproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]) - Example, \r\n" +
                     "DA0*ABS-WMSLDM.S$PROC/WMS-LDMIP-0"),
            Category("Development Environment"), DefaultValue("")]
        public string DEVschemaFile { get; set; }

        #endregion
        #region User Test Environment
        [Description("Source file name (qual*file) - Example, \r\n" +
                     "IM5*LDMIPSRC"),
            Category("User Test Environment"), DefaultValue("")]
        public string TSTSRCFile { get; set; }

        [Description("Proc 1 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY"),
            Category("User Test Environment"), DefaultValue("")]
        public string TSTproc1File { get; set; }

        [Description("Proc 2 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY2"),
            Category("User Test Environment"), DefaultValue("")]
        public string TSTproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]), Example: \r\n" +
                     "DA0*ABS-WMSLDM.S$PROC/WMS-LDMIP-0"),
            Category("User Test Environment"), DefaultValue("")]
        public string TSTschemaFile { get; set; }
        #endregion

        #region Pseudo Environment
        [Description("Source file name (qual*file) - Example, \r\n" +
                     "LDMIP*PRDSRC"),
            Category("Pseudo Environment"), DefaultValue("")]
        public string PSDSRCFile { get; set; }

        [Description("Proc 1 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY"),
            Category("Pseudo Environment"), DefaultValue("")]
        public string PSDproc1File { get; set; }

        [Description("Proc 2 - Proc file name (qual*file) - Example, \r\n" +
                     "IM5*MIPCOPY2"),
            Category("Pseudo Environment"), DefaultValue("")]
        public string PSDproc2File { get; set; }

        [Description("Schema - Schema file name (qual*file.element[/version]), Example: \r\n" +
                     "DA0*ABS-WMSLDM.S$PROC/WMS-LDMIP-0"),
            Category("Pseudo Environment"), DefaultValue("")]
        public string PSDschemaFile { get; set; }

        #endregion

        
    }
}

