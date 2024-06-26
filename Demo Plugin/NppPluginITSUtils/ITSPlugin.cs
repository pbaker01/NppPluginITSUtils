﻿// NPP plugin platform for .Net v0.91.57 by Kasper B. Graversen etc.
using System;
using System.IO;
using System.Text;
using Kbg.NppPluginNET.PluginInfrastructure;
using System.Windows.Forms;
  // using System.Text.RegularExpressions;
using static ITS.Utils.ITSENums;
using static ITS.Utils.ITSConstants;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using ITS.Utils;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;

namespace Kbg.NppPluginNET
{
    /// <summary>
    /// AIA Technologies - 2024
    /// 
    /// This plugin is designed to work in shops running Unisys 2200 
    /// ACOB or UCOB programs.
    /// 
    /// This plugin includes the following features:
    ///   - Togggle COBOL comment
    ///   - Load Proc definitions from configured ACOB proc filess, 
    ///     UCOM proc file, or system proc file (for procs that use DPS, etc).
    ///   - Load DMS record descriptions.   
    ///   - Load program (elements) from applications or personal workspace 
    ///     SRC files.
    /// 
    /// The plug was built using the templates and sample code here:
    /// https://github.com/kbilsted/NotepadPlusPlusPluginPack.Net
    ///   - This code was used as the template
    ///   
    /// https://github.com/molsonkiko/JsonToolsNppPlugin
    ///   - This code was used to get an example of how to display a dialog.
    /// 
    /// 
    /// </summary>
    class Main
    {
        static IScintillaGateway editor;
        static INotepadPPGateway notepad = new NotepadPPGateway();

        static HashSet<IntPtr> openedFiles = new HashSet<IntPtr>();

        static internal void CommandMenuInit() 
        {
            Kbg.Demo.Namespace.Main.CommandMenuInit();
        }

        static internal void PluginCleanUp()
        {
            Kbg.Demo.Namespace.Main.PluginCleanUp();
        }

        static internal void SetToolBarIcon()
        {
            Kbg.Demo.Namespace.Main.SetToolBarIcon();
        }

        /**
         * Listener for Notifications
         */
        public static void OnNotification(ScNotification notification) {
            uint code = notification.Header.Code;

            switch (code) {
                case (uint) NppMsg.NPPN_FILEOPENED: {
                        openedFiles.Add(notification.Header.IdFrom);
                        break;
                    }
                case (uint) NppMsg.NPPN_BUFFERACTIVATED: {
                        if (openedFiles.Contains(notification.Header.IdFrom)) {
                            openedFiles.Remove(notification.Header.IdFrom);
                            editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
                            int langType = (int) Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETBUFFERLANGTYPE, notification.Header.IdFrom, 0);
                            string textBlock = editor.GetText(1600);
                            if (langType == 0) {
                                if (textBlock.Contains(ITSConstants.IDENTIFICATION_DIVISION)) {
                                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETBUFFERLANGTYPE, notification.Header.IdFrom, (int)LangType.L_COBOL);
                                } else if (textBlock.Substring(0,Math.Min(textBlock.Length,80)).Contains(ITSConstants.PROC)) {
                                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETBUFFERLANGTYPE, notification.Header.IdFrom, (int)LangType.L_COBOL);
                                }
                            }
                        }
                        break;
                    }
            }
            return;
        }

        internal static string PluginName { get { return Kbg.Demo.Namespace.Main.PluginName; }}
    }
}

namespace Kbg.Demo.Namespace {
    class Main {
        #region " Fields "
        internal const string PluginName = "ITSPlugin";
        static string iniFilePath = null;
        static string SCREEN_PREFIX = "SCREEN-";
        static string COBP_VERSION = "COBP";
        static char FWD_SLASH_CHAR = '/';
        static char HASH_CHAR = '#';
        static char EQUALS_CHAR = '=';
        static char ASTERISK_CHAR = '*';
        static char PERIOD_CHAR = '.';
        static string PERIOD_STRING = PERIOD_CHAR.ToString();

        static string SET = "SET";
        static string AREA = "AREA";
        static string RECORD = "RECORD";

        static string copyPattern = "^...... +COPY *([A-Z0-9-]+)\\. *$";
        static string sourceComputerPattern = "^...... +SOURCE-COMPUTER *\\. *([A-Z0-9-]+)\\. *$";
        static Regex copyRegEx = new Regex(copyPattern, RegexOptions.IgnoreCase);
        static Regex sourceComputerRegEx = new Regex(sourceComputerPattern, RegexOptions.IgnoreCase);

        // general stuff things
        public static JSON_Tools.Utils.Settings settings = new JSON_Tools.Utils.Settings();

        static IScintillaGateway editor = new ScintillaGateway(PluginBase.GetCurrentScintilla());
        static INotepadPPGateway notepad = new NotepadPPGateway();

        // Used on calls to worker functions. 
        struct ITSStatus {
            public bool errorOccured;
            public int errorNum;
            public string errorText;
        }

        struct debugContext {
            public bool workingStorageFound;
            public bool procedureDivisionFound;
            public bool fatalError;
            public int lineNum;
        }


        static bool SET_READ_ONLY = true;
        static bool DO_NOT_SET_READ_ONLY = !SET_READ_ONLY;

        static string ALIAS_FILE_NAME = "aliasEltName.ini";

        #endregion

        #region " Startup/CleanUp "

        /*
         * The function is what contriols what is displayed when the user
         * selects Plugins | ITSPlugin 
         */
        static internal void CommandMenuInit() {
            // Initialization of your plugin commands
            // You should fill your plugins commands here

            //
            // Firstly we get the parameters from your plugin config file (if any)
            //

            // get path of plugin configuration
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();

            // if config path doesn't exist, we create it
            if (!Directory.Exists(iniFilePath)) {
                Directory.CreateDirectory(iniFilePath);
            }

            // make your plugin config file full file path name
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            // with function :
            // SetCommand(int index,                            // zero based number to indicate the order of command
            //            string commandName,                   // the command name that you want to see in plugin menu
            //            NppFuncItemDelegate functionPointer,  // the symbol of function (function pointer) associated with this command. The body should be defined below. See Step 4.
            //            ShortcutKey *shortcut,                // optional. Define a shortcut to trigger this command
            //            bool check0nInit                      // optional. Make this menu item be checked visually
            //            );
            PluginBase.SetCommand(0, "Settings", OpenSettings);
            PluginBase.SetCommand(1, "Edit Alias File", EditAliasFile);
            PluginBase.SetCommand(2, "---", null);
            PluginBase.SetCommand(3, "Toggle COBOL Comment", toggleCobolComment, new ShortcutKey(true, true, true, Keys.C));
            PluginBase.SetCommand(4, "Add Author's initials and MMYY", addAuthorDate);
            PluginBase.SetCommand(5, "Add xxTEST in sequence column", addXXTest);
            PluginBase.SetCommand(6, "Show Comment Lines", showCommentLines);
            PluginBase.SetCommand(7, "Show Comment Lines", showCommentLines);
            PluginBase.SetCommand(8, "---", null);
            PluginBase.SetCommand(9, "View ACOB COBOL Proc", loadACOBCOBOLProc, new ShortcutKey(true, true, true, Keys.A));
            PluginBase.SetCommand(10, "View UCOB COBOL Proc", loadUCOBCOBOLProc, new ShortcutKey(true, true, true, Keys.U));
            PluginBase.SetCommand(11, "View System Proc (DPS)", loadSystemProc, new ShortcutKey(true, true, true, Keys.S));
            PluginBase.SetCommand(12, "---", null);
            PluginBase.SetCommand(13, "View DMS Schema Area", viewDMSSchemaArea);
            PluginBase.SetCommand(14, "View DMS Schema Set", viewDMSSchemaSet);
            PluginBase.SetCommand(15, "View DMS Schema Record", viewDMSSchemaRecord);
            PluginBase.SetCommand(16, "---", null);
            PluginBase.SetCommand(17, "View Program/Element from Lcl Workspace.", loadEltFromWorkspace, new ShortcutKey(true, true, true, Keys.W));
            PluginBase.SetCommand(18, "View Program/Element from Env SRC file.", loadEltFromSRCFile, new ShortcutKey(true, true, true, Keys.F));
            PluginBase.SetCommand(19, "---", null);
            PluginBase.SetCommand(20, "Find Working Storage.", findWorkingStorageField, new ShortcutKey(true, true, true, Keys.E));
            PluginBase.SetCommand(21, "Find Paragraph Name.", findParagraphName, new ShortcutKey(true, true, true, Keys.P));
            PluginBase.SetCommand(22, "---", null);
            PluginBase.SetCommand(23, "Add Display Lines", addDisplayLines);
            PluginBase.SetCommand(24, "Delete xxTEST Lines", deletexxTESTLines);
            PluginBase.SetCommand(25, "---", null);
            PluginBase.SetCommand(26, "Expand Copy Statements", expandCopyStmts);
        }

        static internal void SetToolBarIcon() { }
        public static void OnNotification(ScNotification notification) { }
        static internal void PluginCleanUp() { }
        #endregion

        #region " Menu functions "

        //  Display the settings dialog.
        //  Allows user to configure required file names. 
        static void OpenSettings() {
            settings.ShowDialog("Settings for NY ITS NPP Plugin");
        }

        /**
         * This function expands all of the COPY statements found in the COBOL program.
         * Note, there is no check if the file is actually a COBOL program.
         * 
         * There are several PROC files and the order these files are search varys
         * depending on the SOURCE-COMPUTER. statement.  If UCOB or BOTH follows
         * SOURCE-COMPUTER then the UCOB procfile is searched ahead of the ACOB
         * procs.  The system proc file is always searched last. If the 
         * SOURCE-COMPUTER statement is not found or can't be processed the processing
         * defaults to ACOB search order where the ACOB proc files (1 and 2) are searched
         * first.
         * 
         **/
        static void expandCopyStmts() {
            Dictionary<string, string> aliasDictionary = loadAliasDictionary();
            Dictionary<int, string> procFileDictionary = loadProcFileDictionary();

            string readPath = notepad.GetCurrentFilePath();
            // string writePath = Path.GetTempFileName();
            string writePath = getScratchFilePath(Path.GetFileNameWithoutExtension(readPath), Path.GetExtension(readPath)) ;
            if (writePath == null) {
                MessageBox.Show(errorText[(int) ERRORS.ITSERR020]);
                return;
            }

            const Int32 BufferSize = 512;

            StreamWriter writer;

            try {
                writer = new StreamWriter(writePath);
                using (var fileStream = File.OpenRead(readPath))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                    String line;
                    while ((line = streamReader.ReadLine()) != null) {
                        processCOBOLLine(line, writer, aliasDictionary, procFileDictionary);
                    }
                    writer.Close();
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            notepad.OpenFile(writePath);
        }

        static void processCOBOLLine(string pLine, StreamWriter pWriter, Dictionary<string, string> pAliasDictionary, Dictionary<int, string> procFileDictionary) {
            Match m = null;
            Group g = null;
            char[] lineTextAsChars;
            string copyProcName;
            string sourceComputerName;
            string copyProcEltName;
            bool sourceComputerProcessed = false;

            try {
                if (!sourceComputerProcessed) {
                    m = sourceComputerRegEx.Match(pLine);
                    if (m.Success) {
                        sourceComputerProcessed = true;
                        pWriter.WriteLine(pLine);
                        g = m.Groups[1];
                        CaptureCollection cc = g.Captures;
                        Capture c = cc[0];
                        sourceComputerName = c.ToString();
                        if (sourceComputerName.ToUpper().Equals(SC_UCOB) ||
                            sourceComputerName.ToUpper().Equals(SC_BOTH)) {
                            pWriter.WriteLine(String.Format(EXPMSG005, sourceComputerName));
                            adjProcSearchOrder(procFileDictionary, pWriter);
                        }
                        writeProcFileSearchOrder(procFileDictionary, pWriter);
                        return;
                    }
                }
                
                m = copyRegEx.Match(pLine);
                if (m.Success) {
                    g = m.Groups[1];
                    CaptureCollection cc = g.Captures;
                    Capture c = cc[0];
                    copyProcName = c.ToString();

                    lineTextAsChars = pLine.ToCharArray();
                    lineTextAsChars[6] = ASTERISK_CHAR;
                    pLine = new string(lineTextAsChars);
                    pWriter.WriteLine(pLine);
                    if (pAliasDictionary.ContainsKey(copyProcName)) {
                        copyProcEltName = pAliasDictionary[copyProcName];
                    } else {
                        // Check if requesting a DPS screen.
                        // The screen proc contains two procs: the screen and the INIT procs
                        if (copyProcName.StartsWith(SCREEN_PREFIX) && (!copyProcName.Contains(FWD_SLASH_CHAR.ToString()))) {
                                string[] elementNamePart = copyProcName.Split('-');
                                if (elementNamePart.Length == 4) {
                                    copyProcEltName = elementNamePart[0] + "-" + elementNamePart[elementNamePart.Length - 2] + FWD_SLASH_CHAR + COBP_VERSION;
                                }
                                else if (elementNamePart.Length == 3) {
                                    copyProcEltName = elementNamePart[0] + "-" + elementNamePart[elementNamePart.Length - 1] + FWD_SLASH_CHAR + COBP_VERSION;
                                } else {
                                    copyProcEltName = copyProcName;
                                }
                        } else {
                            copyProcEltName = copyProcName;
                        }
                    }
                    processCOPYLine(copyProcName, copyProcEltName, pWriter, procFileDictionary);

                } else {
                    pWriter.WriteLine(pLine);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        static void processCOPYLine(string pCopyProcName, string pCopyProcEltName, StreamWriter pWriter, Dictionary<int, string> pProcFileDictionary) {
            string copyProcFilePath = "";
            bool found = false;

            for (int i = 0; i < 4; i++) {
                if (pProcFileDictionary.ContainsKey(i)) {
                    // Replace a forward slash with a "." to indicate extension (version in Unisys)
                    string eltFileFmt = pCopyProcEltName.Replace(FWD_SLASH_CHAR, PERIOD_CHAR);
                    copyProcFilePath = pProcFileDictionary[i] + eltFileFmt;
                    if (File.Exists(copyProcFilePath)) {
                        insertCopyProc(copyProcFilePath, pCopyProcName, pWriter);
                        found = true;
                        break;
                    }
                } 
            }

            if (!found) {
                // If it is a DPS proc is may need the COBP version. 
                if (!pCopyProcEltName.Contains(FWD_SLASH_CHAR.ToString())) {
                    copyProcFilePath = pProcFileDictionary[(int) PROC_SEARCH_ORDER.SEARCH_FILE_4] + pCopyProcEltName + FWD_SLASH_CHAR + COBP_VERSION;
                    if (File.Exists(copyProcFilePath)) {
                        insertCopyProc(copyProcFilePath, pCopyProcName, pWriter);
                        found = true;
                        // If there is no fwd slash then concatenate "/COBP" to elemenent name. 
                    }
                }
            }

            if (!found) {
                pWriter.WriteLine(String.Format(EXPMSG003, pCopyProcName));
            } else {
                pWriter.WriteLine(string.Format(EXPMSG004, pCopyProcName, copyProcFilePath));
            }
        }

        static void insertCopyProc(string pCopyProcFilePath, string pCopyProcName, StreamWriter pWriter) {

            Int32 BufferSize = 512;
            bool strProcFound = false;


            try {
                using (var fileStream = File.OpenRead(pCopyProcFilePath))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                    string line;
                    while (true) {
                        line = streamReader.ReadLine();
                        if (line == null) {
                            break;
                        }

                        if (!strProcFound) {
                            if (line.StartsWith(pCopyProcName)) {
                                strProcFound = true;
                                continue;
                            }
                        } else {
                            if (line.Trim() == ITSConstants.END_STMT) {
                                break;
                            }
                            else {
                                pWriter.WriteLine(line);
                            }
                        }
                    }

                    if (!strProcFound) {
                        pWriter.WriteLine(String.Format(EXPMSG002, pCopyProcName, pCopyProcFilePath));
                    }

                    return;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return;
            }
        }


        static Dictionary<string, string> loadAliasDictionary() {

            Dictionary<string, string> aliasDictionary = new Dictionary<string, string>();


            string path = notepad.GetNppPath();
            path = path + ALIAS_FILE_NAME;

            // If there is no alias file then just exit with alias = null
            if (!File.Exists(path)) {
                return aliasDictionary;
            }

            Int32 BufferSize = 512;

            // Read alias file and search for key = pEltName. 
            // When found return alias.
            try {
                using (var fileStream = File.OpenRead(path))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                    string line;
                    while (true) {
                        line = streamReader.ReadLine();
                        if (line == null) {
                            break;
                        }

                        if (line.Trim().Length == 0 || line.TrimStart().StartsWith(HASH_CHAR.ToString())) {
                            continue; // skip this line 
                        }

                        line = line.Trim();
                        string[] names = line.Split(EQUALS_CHAR);

                        if (names.Length != 2) {
                            continue;
                        }

                        aliasDictionary.Add(names[0].Trim().ToUpper(), names[1].Trim().ToUpper());
                    }
                    return aliasDictionary;
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return aliasDictionary;
            }
        }

        static Dictionary<int, string> loadProcFileDictionary() {

            Dictionary<int, string> procFileDictionary = new Dictionary<int, string>();
            string procFilePath;

            if (settings.workingEnvt == ENVIRONMENT.Development) {
                procFilePath = getProcFilePath(settings.DEVACOBproc1File);  
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_1, procFilePath);
                procFilePath = getProcFilePath(settings.DEVACOBproc2File);
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_2, procFilePath);
                procFilePath = getProcFilePath(settings.DEVUCOBprocFile);
                if (procFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_3, procFilePath);
            }
            else if (settings.workingEnvt == ENVIRONMENT.UserTest) {
                procFilePath = getProcFilePath(settings.TSTACOBproc1File);
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_1, procFilePath);
                procFilePath = getProcFilePath(settings.TSTACOBproc2File);
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_2, procFilePath);
                procFilePath = getProcFilePath(settings.TSTUCOBprocFile);
                if (procFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_3, procFilePath);
            }
            else if (settings.workingEnvt == ENVIRONMENT.Pseudo) {
                procFilePath = getProcFilePath(settings.PSDACOBproc1File);
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_1, procFilePath);
                procFilePath = getProcFilePath(settings.PSDACOBproc2File);
                if (procFilePath != null) procFileDictionary.Add((int) PROC_SEARCH_ORDER.SEARCH_FILE_2, procFilePath);
                procFilePath = getProcFilePath(settings.PSDUCOBprocFile);
                if (procFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_3, procFilePath);
            }

            procFilePath = getProcFilePath(settings.systemProcFile);
            if (procFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_4, procFilePath);

            return procFileDictionary;
        }

        static void adjProcSearchOrder(Dictionary<int, string> procFileDictionary, StreamWriter pWriter) {

            string acobProcFile1Path = null;
            string acobProcFile2Path = null;
            string ucobProcFilePath = null;
            string sysProcFilePath = null;

            // Current order: ACOB-P1 ACOB-P2 UCOB SYSPROC
            // New order:     UCOB ACOB-P1 ACOB-P2 SYSPROC

            if (procFileDictionary.ContainsKey((int) PROC_SEARCH_ORDER.SEARCH_FILE_1)) {
                acobProcFile1Path = procFileDictionary[(int)PROC_SEARCH_ORDER.SEARCH_FILE_1];
                procFileDictionary.Remove((int)PROC_SEARCH_ORDER.SEARCH_FILE_1);
            }

            if (procFileDictionary.ContainsKey((int)PROC_SEARCH_ORDER.SEARCH_FILE_2)) {
                acobProcFile2Path = procFileDictionary[(int)PROC_SEARCH_ORDER.SEARCH_FILE_2];
                procFileDictionary.Remove((int)PROC_SEARCH_ORDER.SEARCH_FILE_2);
            }

            if (procFileDictionary.ContainsKey((int)PROC_SEARCH_ORDER.SEARCH_FILE_3)) {
                ucobProcFilePath = procFileDictionary[(int)PROC_SEARCH_ORDER.SEARCH_FILE_3];
                procFileDictionary.Remove((int)PROC_SEARCH_ORDER.SEARCH_FILE_3);
            }

            if (procFileDictionary.ContainsKey((int)PROC_SEARCH_ORDER.SEARCH_FILE_4)) {
                sysProcFilePath = procFileDictionary[(int)PROC_SEARCH_ORDER.SEARCH_FILE_4];
                procFileDictionary.Remove((int)PROC_SEARCH_ORDER.SEARCH_FILE_4);
            }

            if (ucobProcFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_1, ucobProcFilePath);
            if (acobProcFile1Path != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_2, acobProcFile1Path);
            if (acobProcFile2Path != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_3, acobProcFile2Path);
            if (sysProcFilePath != null) procFileDictionary.Add((int)PROC_SEARCH_ORDER.SEARCH_FILE_4, sysProcFilePath);
        }

        static void writeProcFileSearchOrder(Dictionary<int, string> procFileDictionary, StreamWriter pWriter) {
            pWriter.WriteLine(EXPMSG001);
            for (int i = 0; i < 4; i++) {
                if (procFileDictionary.ContainsKey(i)) {
                    pWriter.WriteLine(String.Format(EXPMSG006, i, procFileDictionary[i]));
                }
            }
        }


        static string getProcFilePath(string pOS2200ProcFileName) { 
            string procFilePath = null;

            if (pOS2200ProcFileName == null || pOS2200ProcFileName.Trim().Length == 0) {
                return procFilePath;
            }

            string[] fileParts = pOS2200ProcFileName.Split(ASTERISK_CHAR);
            int numAsterisks = fileParts.Length - 1;
            if (numAsterisks != 1) {
                return procFilePath;
            }

            // Replace * in program file name with a back slash
            string pgmFileFmt = pOS2200ProcFileName.Replace(ASTERISK_CHAR, '\\');

            // Build path to file/element by adding the mapped drive letter.
            char driveLetter = (char)('A' + settings.mappedDrive);
            procFilePath = (driveLetter + ":") + "\\" + pgmFileFmt + "\\";

            return procFilePath;
        }

        static string getScratchFilePath(string pFileName, string pFileExt) {
            string scratchPath = settings.scratchFolderPath;

            if (scratchPath == null || scratchPath.Length == 0) {
                return null;
            }

            if (pFileName != null && pFileName.Length > 0) {
                pFileName = pFileName + "-EXP";
            } else {
                pFileName = "NPP-EXP";
            }

            scratchPath = scratchPath + pFileName;
            if (pFileExt != null || pFileExt.Length > 0) {
                scratchPath = scratchPath + pFileExt;
            }

            return scratchPath;
        }



        //  Display the settings dialog.
        //  Allows user to configure required file names. 
        static void EditAliasFile() {

            string path = notepad.GetNppPath();
            path = path + ALIAS_FILE_NAME;

            if (!File.Exists(path)) {

                try {
                    // Create the file, or overwrite if the file exists.
                    using (FileStream fs = File.Create(path)) {
                        byte[] info = new UTF8Encoding(true).GetBytes("# Enter Key = Value pairs.");
                        // Add some information to the file.
                        fs.Write(info, 0, info.Length);
                        info = new UTF8Encoding(true).GetBytes("# key = selected text, value = element name (with /version.");
                        // Add some more information to the file.
                        fs.Write(info, 0, info.Length);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }

            notepad.OpenFile(path);
        }

        /*
         * Working on this one.. 
         * 
         */
        static void findWorkingStorageField() {
            ITSStatus status = new ITSStatus();
            // Get the selected text.
            string dataName = getSelectedText();
            if (dataName == null || dataName.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = errorText[(int)ERRORS.ITSERR011];
                showError(status.errorText);
                return;
            }

            string searchStr = "^.{0,6} *[0-9]{1,2} *" + dataName;

            int curLine = editor.GetCurrentLineNumber();
            int fstVisableLine = editor.GetFirstVisibleLine();
            CharacterRange cr = new CharacterRange(0, editor.GetLength());
            TextToFind ttf = new TextToFind(cr, searchStr);
            int findPos = editor.FindText(FindOption.REGEXP, ttf);
            if (findPos == -1) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR017;
                status.errorText = string.Format(errorText[status.errorNum], dataName);
                showError(status.errorText);
                return;
            }

            // Data element found!
            editor.MarkerAdd(curLine, 20);

            int lineNum = editor.LineFromPosition(findPos);
            editor.SetFirstVisibleLine(lineNum);

            return;
        }


        /*
 * Working on this one.. 
 * 
 */
        static void findParagraphName() {
            ITSStatus status = new ITSStatus();
            // Get the selected text.
            string paragraphName = getSelectedText();
            if (paragraphName == null || paragraphName.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = errorText[(int)ERRORS.ITSERR011];
                showError(status.errorText);
                return;
            }

            string searchStr = "^.{0,6} *" + paragraphName + "\\.";

            int curLine = editor.GetCurrentLineNumber();
            int fstVisableLine = editor.GetFirstVisibleLine();
            CharacterRange cr = new CharacterRange(0, editor.GetLength());
            TextToFind ttf = new TextToFind(cr, searchStr);
            int findPos = editor.FindText(FindOption.REGEXP, ttf);
            if (findPos == -1) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR018;
                status.errorText = string.Format(errorText[status.errorNum], paragraphName);
                showError(status.errorText);
                return;
            }

            // Paragraph found!
            // Set marker 
            editor.MarkerAdd(curLine, 20);
            int lineNum = editor.LineFromPosition(findPos);
            editor.SetFirstVisibleLine(lineNum);

            return;
        }

        static void hideCommentLines() {
            string line;
            int cmtStr = 0;
            int cmtEnd = 0;

            for (int i = 0; i < editor.GetLineCount(); i++) {
                line = editor.GetLine(i).TrimEnd();
                if (line.Length > 6) {
                    if (line[6] == '*') {
                        if (cmtStr == 0) {
                            cmtStr = i;
                            cmtEnd = i;
                        } else {
                            cmtEnd = i;
                        }
                        // editor.HideLines(i, i);
                    } else if (cmtStr != 0) {
                        editor.HideLines(cmtStr, cmtEnd);
                        editor.HideLines(cmtStr, cmtEnd);
                        cmtStr = 0;
                        cmtEnd = 0;
                    }
                }
            }
        }

        static void showCommentLines() {
            editor.ShowLines(0, editor.GetLineCount() - 1);
        }

        static void processFileOpened(int pBufferID) {
            string path = GetFilePath(pBufferID);

            // If this file has an extension then
            string extension;
            extension = Path.GetExtension(path);
            if (Path.GetExtension(path).Length > 0)
                return;

            // Change buffer language type to COBOL - agr1 is BufferID arg2 is LangType
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_SETBUFFERLANGTYPE, pBufferID, (int)LangType.L_COBOL);
        }

        static string GetFilePath(int bufferId) {
            var path = new StringBuilder(2000);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLPATHFROMBUFFERID, bufferId, path);
            return path.ToString();
        }



        static void addDisplayLines() {

            debugContext debugInfo = new debugContext();
            debugInfo.procedureDivisionFound = false;
            debugInfo.lineNum = 0;

            string currLang = editor.GetLexerLanguage();

            while (debugInfo.lineNum < editor.GetLineCount() &&
                  !debugInfo.fatalError) {
                debugInfo = processLine(debugInfo);
            }

            processPD(debugInfo);

        }

        // static displayContext 

        static debugContext processLine(debugContext debugInfo) {
            string lineText;
            char[] lineTextAsChars;

            int lineLen;

            var strPos = editor.PositionFromLine(debugInfo.lineNum);
            var endPos = editor.GetLineEndPosition(debugInfo.lineNum);
            editor.SetTargetRange(strPos, endPos);


            // Get the current line length
            lineLen = endPos - strPos;

            // Skip lines that are empty.
            if (lineLen <= 6) {
                debugInfo.lineNum = debugInfo.lineNum + 1;
                return debugInfo;
            }

            lineText = editor.GetLine(debugInfo.lineNum);
            lineTextAsChars = lineText.ToCharArray();

            // Is this a comment line?
            if (lineTextAsChars[6] == ASTERISK_CHAR) {
                debugInfo.lineNum = debugInfo.lineNum + 1;
                return debugInfo;
            }

            if (!debugInfo.workingStorageFound) {
                string cobolCmd = lineText.Substring(7).Trim();
                if (cobolCmd.Length > WORKING_STORAGE_SECTION.Length &&
                    cobolCmd.StartsWith(WORKING_STORAGE_SECTION)) {
                    debugInfo.workingStorageFound = true;
                    debugInfo=processWS(debugInfo);
                    return debugInfo;
                }
                debugInfo.lineNum = debugInfo.lineNum + 1;
                return debugInfo;
            }


            if (!debugInfo.procedureDivisionFound) {
                string cobolCmd = lineText.Substring(7).Trim();
                if (cobolCmd.Length > PROCEDURE_DIVISION.Length &&
                    cobolCmd.StartsWith(PROCEDURE_DIVISION)) {
                    debugInfo.procedureDivisionFound = true;
                    debugInfo.lineNum = debugInfo.lineNum + 1;
                    return debugInfo;
                }
                debugInfo.lineNum = debugInfo.lineNum + 1;
                return debugInfo;
            }

            // Procedure Division has been found!

            return processPG(debugInfo, lineText);
        }

        static debugContext processWS(debugContext debugInfo) {

            if (true) return debugInfo;

            string wsFile = "cobolDspWS.cbl";

            string templatePath = notepad.GetPluginConfigPath() + "\\ITSPlugin\\" + wsFile;

            if (!File.Exists(templatePath)) {
                showError(string.Format(errorText[(int) ERRORS.ITSERR019], wsFile, templatePath));
                debugInfo.fatalError = true;
                return debugInfo;
            }

            string punchCardCol = getInitials() + TEST;

            // COBOL Working Storage Template exists. Open and Read.

            Int32 BufferSize = 1028;
            using (var fileStream = File.OpenRead(templatePath))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                string line;
                // Loop through COBOL Template File
                while (!streamReader.EndOfStream) {
                    // Get next line
                    line = streamReader.ReadLine();
                    line = Regex.Replace(line, @"\t|\n|\r", "");
                    line = punchCardCol + line.Substring(6);
                    debugInfo.lineNum = debugInfo.lineNum+1;
                    editor.GotoLine(debugInfo.lineNum);
                    editor.NewLine();
                    int strPos = editor.PositionFromLine(debugInfo.lineNum);
                    int endPos = editor.GetLineEndPosition(debugInfo.lineNum);
                    editor.SetTargetRange(strPos, endPos);
                    editor.ReplaceTarget(line.Length, line);
                }
            }
            return debugInfo;
        }

        static debugContext processPD(debugContext debugInfo) {

            if (true) return debugInfo;

            string wsFile = "cobolDspPD.cbl";

            string templatePath = notepad.GetPluginConfigPath() + "\\ITSPlugin\\" + wsFile;

            if (!File.Exists(templatePath)) {
                showError(string.Format(errorText[(int)ERRORS.ITSERR019], wsFile, templatePath));
                debugInfo.fatalError = true;
                return debugInfo;
            }

            string punchCardCol = getInitials() + TEST;

            // COBOL Working Storage Template exists. Open and Read.
            debugInfo.lineNum = editor.GetLineCount();
            editor.GotoLine(debugInfo.lineNum);
            Int32 BufferSize = 1028;
            editor.NewLine();
            using (var fileStream = File.OpenRead(templatePath))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                string line;
                // Loop through COBOL Template File
                while (!streamReader.EndOfStream) {
                    // Get next line
                    line = streamReader.ReadLine();
                    line = Regex.Replace(line, @"\t|\n|\r", "");
                    line = punchCardCol + line.Substring(6);
                    debugInfo.lineNum = debugInfo.lineNum + 1;
                    editor.GotoLine(debugInfo.lineNum);
                    editor.NewLine();
                    // editor.AppendText(line.Length, line);
                    int strPos = editor.PositionFromLine(debugInfo.lineNum);
                    int endPos = editor.GetLineEndPosition(debugInfo.lineNum);
                    editor.SetTargetRange(strPos, endPos);
                    editor.ReplaceTarget(line.Length, line);
                }
            }
            return debugInfo;
        }


        static debugContext processPG(debugContext debugInfo, string lineText) {
            var strPos = editor.PositionFromLine(debugInfo.lineNum);
            var endPos = editor.GetLineEndPosition(debugInfo.lineNum);

            string pattern1 = @"^.{7,7}([A-Za-z0-9_-]+)\. *EXIT\.";
            string pattern2 = @"^.{7,7}([A-Za-z0-9_-]+)\.";
            Regex r1 = new Regex(pattern1, RegexOptions.IgnoreCase);
            Regex r2 = new Regex(pattern2, RegexOptions.IgnoreCase);
            Match m = null;
            Group g = null;


            // Is this the begining of the requested record?
            m = r1.Match(lineText);

            // @TODO Need to implement this.. 
            if (m.Success) {
                debugInfo.lineNum = debugInfo.lineNum+1;
                return debugInfo;
            }

            m = r2.Match(lineText);
            if (m.Success) {
                g = m.Groups[1];
                CaptureCollection cc = g.Captures;
                Capture c = cc[0];

                bool multiLine = false;
                if (29 + 15 + c.ToString().Length > 72) {
                    multiLine = true;
                }

                string tstComment = getInitials() + "TEST";

                editor.GotoLine(debugInfo.lineNum+1);
                
                // Check for EXIT. on next line.  Note does not check for comment line. 
                if (editor.GetLine(debugInfo.lineNum + 1).Length >= 8 && 
                    editor.GetLine(debugInfo.lineNum+1).Substring(7).Trim().StartsWith("EXIT.")) {
                    debugInfo.lineNum = debugInfo.lineNum+1;
                    return debugInfo;
                }

                // If paragraph ends with "EXIT" then skip it. 
                if (c.ToString().EndsWith("EXIT")) {
                    debugInfo.lineNum = debugInfo.lineNum + 1;
                    return debugInfo;
                }

                editor.NewLine();
                strPos = editor.PositionFromLine(debugInfo.lineNum+1);
                endPos = editor.GetLineEndPosition(debugInfo.lineNum+1);
                editor.SetTargetRange(strPos, endPos);
                if (!multiLine) {
                    lineText = tstComment + "     DISPLAY 'Entering " + c.ToString() + "'" + " UPON PRINTER.";
                } else {
                    lineText = tstComment + "     DISPLAY 'Entering " + c.ToString() + "'";
                }
                editor.ReplaceTarget(lineText.Length, lineText);

                if (multiLine) {
                    editor.GotoLine(debugInfo.lineNum + 2);
                    editor.NewLine();
                    strPos = editor.PositionFromLine(debugInfo.lineNum+2);
                    endPos = editor.GetLineEndPosition(debugInfo.lineNum+2);
                    editor.SetTargetRange(strPos, endPos);
                    lineText = "PBTEST         UPON PRINTER.";
                    editor.ReplaceTarget(lineText.Length, lineText);
                    debugInfo.lineNum = debugInfo.lineNum + 3;
                } else {
                    debugInfo.lineNum = debugInfo.lineNum + 2;
                }
               
                return debugInfo;
            }

            debugInfo.lineNum = debugInfo.lineNum+1;
            return debugInfo;
        }

        static void deletexxTESTLines() {

            string tstComment = getInitials() + "TEST";

            int lineNum = 0;
            while (lineNum < editor.GetLineCount()) {
                if (editor.GetLine(lineNum).StartsWith(tstComment)) {
                    editor.GotoLine(lineNum);
                    editor.LineDelete();
                }
                else {
                    lineNum++;
                }
            }
        }

        /*
        * This function iterates through the lines selected and toggle the 
        * comment character in column 7.  If the line length is <= 6 the 
        * line is skipped. If the character at column 7 is a space or 
        * asterisk then toggle the value. If the character is neither a 
        * space or asterisk then skip line.
        */
        static void toggleCobolComment() {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd();

            // Get the line numbers associated with the start and end positions.
            // The strLine and endLine may be the same line.
            var strLine = editor.LineFromPosition(strSel);
            var endLine = editor.LineFromPosition(endSel);

            string lineText;
            char[] lineTextAsChars;

            int lineLen;

            int strPos;
            int endPos;

            // Loop through each line and toggle the comment char "*"
            // in column 7.  Skip lines that are shorter than 7 characters.
            for (var lineNum = strLine; lineNum <= endLine; lineNum++) {
                // Calculate line size by getting the line start and end positions
                strPos = editor.PositionFromLine(lineNum);
                endPos = editor.GetLineEndPosition(lineNum);

                // Get the current line length
                lineLen = endPos - strPos + 1;

                // Skip lines that are empty.
                if (lineLen <= 6)
                    continue;

                // Get line text and convert to char array
                editor.SetTargetRange(strPos, endPos);
                lineText = editor.GetTargetText();
                lineTextAsChars = lineText.ToCharArray();

                // Toggle comment character.
                if (lineTextAsChars[6] == ASTERISK_CHAR)
                    lineTextAsChars[6] = ' ';
                else if (lineTextAsChars[6] == ' ')
                    lineTextAsChars[6] = ASTERISK_CHAR;
                else
                    // The character in column 6 is not a space or asterisk.
                    // Just skip this line. 
                    continue;

                // Convert char array back to string 
                // and update line in editor. 
                lineText = new string(lineTextAsChars);
                editor.ReplaceTarget(lineText.Length, lineText);
            }

            return;
        }

        static void addAuthorDate() {

            string initials = getInitials();

            string chgCmmtString;
            switch (settings.chgFormat) {
                case CHG_CMMT_FORMAT.IIMMYY:
                    // Format IIMMYY i.e. PB0324
                    chgCmmtString = initials + DateTime.Now.ToString("MMy");
                    break;
                case CHG_CMMT_FORMAT.MMYYII:
                    // Format MMYYII i.e. 0324PB
                    chgCmmtString = DateTime.Now.ToString("MMy") + initials;
                    break;
                default:
                    // Format MMYYII i.e. 0324PB
                    chgCmmtString = initials + DateTime.Now.ToString("MMy");
                    break;
            }

            addSeqColComment(chgCmmtString);

            return;
        }

        static void addXXTest() {

            addSeqColComment(getInitials() + "TEST");

            return;
        }



        /*
* This function iterates through the lines selected and toggle the 
* comment character in column 7.  If the line length is <= 6 the 
* line is skipped. If the character at column 7 is a space or 
* asterisk then toggle the value. If the character is neither a  
* space or asterisk then skip line.
*/
        static void addSeqColComment(string pComment) {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd(); 

            // Get the line numbers associated with the start and end positions.
            // The strLine and endLine may be the same line.
            var strLine = editor.LineFromPosition(strSel);
            var endLine = editor.LineFromPosition(endSel);

            // Create string baseed on selected format: iimmyy or mmyyii.

            string lineText;

            int strPos;
            int endPos;

            // Loop through each line and toggle the comment char "*"
            // in column 7.  Skip lines that are shorter than 7 characters.
            for (var lineNum = strLine; lineNum <= endLine; lineNum++) {
                // Calculate line size by getting the line start and end positions
                strPos = editor.PositionFromLine(lineNum);
                endPos = editor.GetLineEndPosition(lineNum);

                // Get line text and convert to char array
                editor.SetTargetRange(strPos, endPos);
                lineText = editor.GetTargetText();

                if (lineText.Length < 6) {
                    lineText = lineText.PadRight(6, ' ');
                }

                lineText = pComment + lineText.Substring(6);

                editor.ReplaceTarget(lineText.Length, lineText);
            }

            return;
        }


        /*
         *  This is used to load a program (or other) element into notepad++
         * 
         */
        static void loadEltFromWorkspace() {
            ITSStatus status;

            string fileName = settings.WorkSpaceSRCFile;
            status = validateFileName(fileName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            status = loadEltFrmFile(fileName, elementName, DO_NOT_SET_READ_ONLY);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            if (!status.errorOccured) {
                // Set to Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_1);
            }
        }

        static void loadEltFromSRCFile() {

            ITSStatus status = new ITSStatus();
            string fileName;

            switch (settings.workingEnvt) {
                case ENVIRONMENT.Development:
                    fileName = settings.DEVSRCFile;
                    break;
                case ENVIRONMENT.UserTest:
                    fileName = settings.TSTSRCFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    fileName = settings.PSDSRCFile;
                    break;
                default:
                    status.errorOccured = true;
                    // Environment not specified.
                    status.errorNum = (int)ERRORS.ITSERR007;
                    status.errorText = errorText[status.errorNum];
                    return;
            }
            status = validateFileName(fileName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            status = loadEltFrmFile(fileName, elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }


            if (!status.errorOccured) {
                // Set to Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_1);
            }
        }

        static void loadACOBCOBOLProc() {
            ITSStatus status = new ITSStatus();

            string procFile1;
            string procFile2;

            switch (settings.workingEnvt) {
                case ENVIRONMENT.Development:
                    procFile1 = settings.DEVACOBproc1File;
                    procFile2 = settings.DEVACOBproc2File;
                    break;
                case ENVIRONMENT.UserTest:
                    procFile1 = settings.TSTACOBproc1File;
                    procFile2 = settings.TSTACOBproc2File;
                    break;
                case ENVIRONMENT.Pseudo:
                    procFile1 = settings.PSDACOBproc1File;
                    procFile2 = settings.PSDACOBproc2File;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR008;
                    status.errorText = errorText[status.errorNum];
                    showError(status.errorText);
                    return;
            }

            loadCOBOLProc(PROC_TYPE.ACOB_Proc, procFile1, procFile2);
        }

        static void loadUCOBCOBOLProc() {

            ITSStatus status = new ITSStatus();

            string procFile1 = "";
            string procFile2 = "";

            switch (settings.workingEnvt) {
                case ENVIRONMENT.Development:
                    procFile1 = settings.DEVUCOBprocFile;
                    break;
                case ENVIRONMENT.UserTest:
                    procFile1 = settings.TSTUCOBprocFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    procFile1 = settings.PSDUCOBprocFile;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR008;
                    status.errorText = errorText[status.errorNum];
                    showError(status.errorText);
                    return;
            }

            loadCOBOLProc(PROC_TYPE.UCOB_Proc, procFile1, procFile2);
        }

        static void loadSystemProc() {

            // ITSStatus status = new ITSStatus(); not needed just now.. 

            string procFile1 = "";
            string procFile2 = "";

            procFile1 = settings.systemProcFile;

            loadCOBOLProc(PROC_TYPE.System_Proc, procFile1, procFile2);
        }


        /*
         * The function reads proc elements from several configured files.
         * 
         * 
         * 
         */

        static void loadCOBOLProc(PROC_TYPE procType, string procFile1, string procFile2) {
            ITSStatus status = new ITSStatus();

            string procFiles = "";   // Used for error reporting.. 


            // Get the selected text.
            string elementName = getSelectedText();
            status = validateElementName(elementName);
            if (status.errorOccured) {
                showError(status.errorText);
                return;
            }

            // Check if requesting a DPS screen.
            // The screen proc contains two procs: the screen and the INIT procs
            if (procType == PROC_TYPE.ACOB_Proc || procType == PROC_TYPE.UCOB_Proc) {
                if (elementName.StartsWith(SCREEN_PREFIX) && (!elementName.Contains(FWD_SLASH_CHAR.ToString()))) {
                    string[] elementNamePart = elementName.Split('-');
                    if (elementNamePart.Length == 4) {
                        elementName = elementNamePart[0] + "-" + elementNamePart[elementNamePart.Length - 2] + FWD_SLASH_CHAR + COBP_VERSION;
                    } else if (elementNamePart.Length == 3) {
                        elementName = elementNamePart[0] + "-" + elementNamePart[elementNamePart.Length - 1] + FWD_SLASH_CHAR + COBP_VERSION;
                    }
                }
            }

            // DPS Procs can have a version name like /COBP 
            if (procType == PROC_TYPE.System_Proc) {
                // If there is no fwd slash then concatenate "/COBP" to elemenent name. 
                if (!elementName.Contains(FWD_SLASH_CHAR.ToString())) {
                    elementName = elementName + FWD_SLASH_CHAR + COBP_VERSION;
                }
            }

            // If both procfiles are empty then show error and exit. 
            if ((procFile1 == null || procFile1.Length == 0) &&
                (procFile2 == null || procFile2.Length == 0)) {
                status.errorOccured = true;

                // SHow error associated with the type of proc requested.
                switch (procType) {
                    case PROC_TYPE.ACOB_Proc:
                        status.errorNum = (int)ERRORS.ITSERR009;
                        break;
                    case PROC_TYPE.UCOB_Proc:
                        status.errorNum = (int)ERRORS.ITSERR014;
                        break;
                    case PROC_TYPE.System_Proc:
                        status.errorNum = (int)ERRORS.ITSERR015;
                        break;
                    default:
                        break;
                }

                status.errorText = errorText[(int)status.errorNum];
                showError(status.errorText);
                return;
            }

            bool found = false;
            if (procFile1 != null && procFile1.Length > 0) {
                procFiles = procFile1; // procFiles - needed for an error message
                status = validateFileName(procFile1);
                if (status.errorOccured) {
                    showError(status.errorText);
                    return;
                }

                status = loadEltFrmFile(procFile1, elementName);
                if (!status.errorOccured) {
                    // If no errors then we are done here.
                    found = true;
                }
                
            }

            if (!found && procFile2 != null && procFile2.Length > 0) {
                // procFiles is needed for error handling
                if (procFiles.Length == 0)
                    procFiles = procFile2;
                else
                    procFiles += ", " + procFile2;

                status = validateFileName(procFile2);
                if (status.errorOccured) {
                    showError(status.errorText);
                    return;
                }

                status = loadEltFrmFile(procFile2, elementName);
                if (status.errorOccured) {
                    if (status.errorNum != (int)ERRORS.ITSERR010) {
                        showError(status.errorText);
                        return;
                    }
                }
            }

            // The only error that will come here is Not Found.. 
            if (status.errorOccured) {
                showError(status.errorText);
                return; }
            else {
                // Set Tab Color 2
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_2);
            }

            return;
        }

        /* This opens a new NPP window with the specified file pFileName.pElementName
         * pFileName will include the file qualifier and file name. i.e. qual*file.
         * The element name may or may not have a version field.  elt or elt/xxxxxx
         * 
         * The read only attribute will only be set when setRadOnly == true (default).
         */
        static ITSStatus loadEltFrmFile(string pFileName, string pElementName, bool setReadOnly = true) {
            ITSStatus status = new ITSStatus();

            string elementName = pElementName.Trim();

            string path = getFilePath(pFileName, elementName);

            if (!File.Exists(path)) {
                // Remove version (i.e. Sys Proc routine adds /COBP)
                int fwdSlashIdx = elementName.IndexOf(FWD_SLASH_CHAR); // Remove version
                if (fwdSlashIdx > 0)
                    elementName = elementName.Substring(0, fwdSlashIdx);
                string alias = getAliasEltName(elementName);
                if (alias != null && alias.Length > 0) {
                    elementName = alias.Replace(FWD_SLASH_CHAR, PERIOD_CHAR).Trim();
                    path = getFilePath(pFileName, elementName);
                    if (!File.Exists(path)) {
                        status.errorOccured = true;
                        status.errorNum = (int)ERRORS.ITSERR012;
                        status.errorText = string.Format(errorText[status.errorNum], pFileName + PERIOD_CHAR + elementName, path);
                        return status;
                    }
                } else {
                    status.errorOccured = true;
                    status.errorNum = (int)ERRORS.ITSERR012;
                    status.errorText = string.Format(errorText[status.errorNum], pFileName + PERIOD_CHAR + elementName, path);
                    return status;
                }
            }

            // Open the element in a new window.
            notepad.OpenFile(path);


            // Set Language to COBOL
            notepad.SetCurrentLanguage(LangType.L_COBOL);

            // Conditionally set ReadOnly on the element just loaded.
            if (setReadOnly) {
                Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
            }
            return status;
        }

        static void viewDMSSchemaArea() {
            viewDMSSchemaFile(AREA);
        }

        static void viewDMSSchemaSet() {
            viewDMSSchemaFile(SET);
        }

        static void viewDMSSchemaRecord() {
            viewDMSSchemaFile(RECORD);
        }

        /*****************************************************************************************
        * Load DMS SET AREA or RECORD                                                                        
        * The user selects the record name in the program and then initiates the Load DMS       
        * record plugin entry point (this function).                                            
        ******************************************************************************************/
        static void viewDMSSchemaFile(string dmsType) {
            ITSStatus status = new ITSStatus();
            bool recFound = false;

            string recordName = getSelectedText().ToUpper();

            status = validateRecordName(recordName);
            if (status.errorOccured) {
                status.errorOccured = true;
                showError(status.errorText);
                return;
            }

            // Get the schema file name based on the environment selected 
            // in the settings. 
            string schemaFile1;
            switch (settings.workingEnvt) {
                case ENVIRONMENT.Development:
                    schemaFile1 = settings.DEVschemaFile;
                    break;
                case ENVIRONMENT.UserTest:
                    schemaFile1 = settings.TSTschemaFile;
                    break;
                case ENVIRONMENT.Pseudo:
                    schemaFile1 = settings.PSDschemaFile;
                    break;
                default:
                    status.errorOccured = true;
                    status.errorText = errorText[(int)ERRORS.ITSERR008];
                    showError(status.errorText);
                    return;
            }

            // Display message if scheama file is not set for selected envt
            if (schemaFile1 == null || schemaFile1.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR004;
                status.errorText = errorText[status.errorNum];
                showError(status.errorText);
                return;
            }

            // Seperate the qualifier and file name from the element name (and version)
            // This is done by spliting the file name on the period. If there are no
            // periods or more than one period and error message is displayed.
            string[] fileName = schemaFile1.Split(PERIOD_CHAR);
            if (fileName.Length != 2) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR013;
                status.errorText = string.Format(errorText[status.errorNum], schemaFile1);
                showError(status.errorText);
                return;
            }

            string path = getFilePath(fileName[0], fileName[1]);

            if (!File.Exists(path)) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR012;
                status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD_CHAR + fileName[1], path);
                showError(status.errorText);
                return;
            }

            // Schema file exists.  Open and Read.

            Int32 BufferSize = 1028;
            using (var fileStream = File.OpenRead(path))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                string pattern1 = String.Format(@"^ *{0} *NAME IS *([a-zA-Z0-9_-]+) *$", dmsType);
                string pattern2 = String.Format(@"^ *{0} *NAME *([a-zA-Z0-9_-]+) *$", dmsType);
                Regex r1 = new Regex(pattern1, RegexOptions.IgnoreCase);
                Regex r2 = new Regex(pattern2, RegexOptions.IgnoreCase);
                Match m = null;
                Group g = null;

                bool eof = false;

                string line;
                // Loop through DMS Schema File
                int lineNum = 0;
                while (!eof) {
                    // Get next line
                    line = streamReader.ReadLine();
                    lineNum += 1;
                    if (line == null) {
                        eof = true;
                        continue;
                    }

                    // If the record has been found determine at begining of a new record. 
                    if (!recFound) {
                        // Is this the begining of the requested record?
                        m = r1.Match(line);
                        if (!m.Success) {
                            m = r2.Match(line);
                            if (!m.Success) {
                                // Not a match... Keep looking.
                                continue;
                            }
                        }
                    }

                    g = m.Groups[1];
                    CaptureCollection cc = g.Captures;
                    Capture c = cc[0];

                    // If it does not match, skip and get next line. 
                    if (!c.ToString().ToUpper().Equals(recordName)) {
                        continue;
                    }

                    // *** DMS RECORD FOUND *** 

                    // Start of DMS record found.  Create a new file in NPP, set language to COBOL
                    // and set the tab color to Orange. 
                    recFound = true;
                    eof = true;
                    notepad.OpenFile(path);
                    // Set Proc Language to COBOL
                    notepad.SetCurrentLanguage(LangType.L_COBOL);
                    // Display line number where record found. 
                    editor.SetFirstVisibleLine(Math.Max(lineNum - 5, 0));
                    // Set read only    
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_EDIT_SETREADONLY);
                    // Set to Tab Color 4
                    Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_VIEW_TAB_COLOUR_3);
                }
            }

            // Specified record name was not found in the schema file. 
            if (!recFound) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR016;
                status.errorText = string.Format(errorText[status.errorNum], fileName[0] + PERIOD_CHAR + fileName[1], path);
                showError(status.errorText);
                return;
            }
            return;
        }

        static string getSelectedText() {
            // Get selection start and end positions.
            // If selStr and selEnd are equal then there is no selection,
            // just process the line the caret is on.
            var strSel = editor.GetSelectionStart();
            var endSel = editor.GetSelectionEnd();

            // Get line text and convert to char array
            editor.SetTargetRange(strSel, endSel);
            string selectedText = editor.GetTargetText().Trim();
            // Make friendly allow period to be included... just truncate. 
            // If there are two periods at the end or other garbage.. Ignored for now. 
            if (selectedText.Length > 1 && selectedText.EndsWith(PERIOD_STRING)) {
                selectedText = selectedText.Substring(0, selectedText.Length - 1);
            }

            return selectedText;
        }

        static ITSStatus validateFileName(string pFileName) {
            ITSStatus status = new ITSStatus();

            if (pFileName == null || pFileName.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR007;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }

            string[] fileParts = pFileName.Split(ASTERISK_CHAR);
            int numAsterisks = fileParts.Length - 1;
            if (numAsterisks == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR005;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }
            else if (numAsterisks > 1) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR006;
                status.errorText = string.Format(errorText[status.errorNum], pFileName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }

        static ITSStatus validateElementName(string pElementName) {
            ITSStatus status = new ITSStatus();

            if (pElementName == null || pElementName.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = string.Format(errorText[status.errorNum], pElementName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }

        static ITSStatus validateRecordName(string pRecordName) {
            ITSStatus status = new ITSStatus();

            if (pRecordName == null || pRecordName.Length == 0) {
                status.errorOccured = true;
                status.errorNum = (int)ERRORS.ITSERR011;
                status.errorText = string.Format(errorText[status.errorNum], pRecordName);
                return status;
            }

            status.errorOccured = false;
            return status;

        }



        static string getFilePath(string programFileName, string elementName) {
            string path = "";

            // Replace * in program file name with a back slash
            string pgmFileFmt = programFileName.Replace(ASTERISK_CHAR, '\\');

            // Replace a forward slash with a "." to indicate extension (version in Unisys)
            string eltFileFmt = elementName.Replace(FWD_SLASH_CHAR, PERIOD_CHAR);

            // Build path to file/element by adding the mapped drive letter.
            char driveLetter = (char)('A' + settings.mappedDrive);
            path = (driveLetter + ":") + "\\" + pgmFileFmt + "\\" + eltFileFmt;

            return path;
        }


        static string getAliasEltName(string pEltName) {

            string alias = null;

            string path = notepad.GetNppPath();
            path = path + ALIAS_FILE_NAME;

            // If there is no alias file then just exit with alias = null
            if (!File.Exists(path)) {
                return alias;
            }

            string eltName = pEltName.Trim().ToLower();

            Int32 BufferSize = 512;

            // Read alias file and search for key = pEltName. 
            // When found return alias.
            try {
                using (var fileStream = File.OpenRead(path))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize)) {
                    string line;
                    while (true) {
                        line = streamReader.ReadLine();
                        if (line == null) {
                            break;
                        }

                        if (line.Trim().Length == 0 || line.TrimStart().StartsWith(HASH_CHAR.ToString())) {
                            continue; // skip this line 
                        }

                        line = line.Trim();
                        string[] names = line.Split(EQUALS_CHAR);

                        if (names.Length != 2) {
                            continue;
                        }

                        if (names[0].Trim().ToLower().Equals(eltName)) {
                            alias = names[1].Trim();
                            break;
                        }
                    }
                    return alias;
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message);
                return alias;
            }
        }

        static void showError(string pErrorTxt) {
            MessageBox.Show(pErrorTxt);
        }

        static string getInitials() {
            string initials = settings.initials.Trim();
            if (initials.Length != 2) {
                if (initials.Length > 2) {
                    initials = initials.Substring(0, 2);
                }
                else if (initials.Length == 1) {
                    initials = initials + "?";
                }
                else {
                    initials = "??";
                }
            }
            return initials;
        }

        static void writeNPPLine(string pLine) {

            int lineNum = editor.GetCurrentLineNumber();

            editor.NewLine();

            if (pLine != null && pLine != "") {

                int strPos = editor.PositionFromLine(lineNum);
                int endPos = editor.GetLineEndPosition(lineNum);

                editor.SetTargetRange(strPos, endPos);
                editor.ReplaceTarget(pLine.Length, pLine);
            }
        }

        /*
         * Used for testing.
         */
        static void updateLine(IScintillaGateway pEditor)
        {

            int strPos = pEditor.PositionFromLine(0);
            int endPos = pEditor.GetLineEndPosition(0);

            string lineText = "This line has been updated";
            pEditor.SetTargetRange(strPos, endPos);
            pEditor.ReplaceTarget(lineText.Length, lineText);
            return;
        }

        #endregion
    }

}   
