﻿using ITS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ITS.Utils.ITSENums;

namespace ITS.Utils {

    public static class ITSConstants {
        public static int msgIndexError = 20;

        /*
         * These are the popup error messages.
         * Why is the first string null? I want to use the ERROR number to 
         * load the message.  I also want the ERROR numbers to start at 1.
         * To avoid having to subtract 1 from the error number to load the
         * message text, I just added a dummy string at the begining of 
         * the table. 
         */
        public static string[] errorText = {
            "", // This msg is not used.
            "ERR001 - Check settings - Workspace file is missing",
            "ERR002 - Check settings - Application SRC file is not defined selected environment.",
            "ERR003 - Check settings - No Proc Files set for selected environment.",
            "ERR004 - Check settings - No Schema file set for selected environment.",
            "ERR005 - Check settings - Unisys program file: {0} is missing the qualifier",
            "ERR006 - Check settings - Unisys program file: {0} contains too many asterisks.",
            "ERR007 - Check settings - Unisys program file (workspace) not specified",
            "ERR008 - Check settings - No environment selected",
            "ERR009 - Check settings - ACOB Proc File 1 and Proc File 2 are not specified for the selected environment.",
            "ERR010 - {0} not found in in File(s) {1}",
            "ERR011 - No text selected",
            "ERR012 - File: {0} not found. Path used: {1}.",
            "ERR013 - Check settings - Schema File format error. File: {0}",
            "ERR014 - Check settings - UCOB Proc File is not specified for the selected environment.",
            "ERR015 - Check settings - System Proc File is not specified.",
            "ERR016 - DMS Record was not found in: {0} using path: {1}",
            "ERR017 - Data element {0} not found.",
            "ERR018 - Paragraph Name {0} not found",
            "ERR019 - {0} not found at path: {1}",
            "ERR020 - Unable to create scratch file. Check scratch folder in settings."
        };

        public static string EXPMSG001 = "      * EXPMSG - Proc File search order.";
        public static string EXPMSG002 = "      * EXPMSG - PROC {0} was not found in file at path: \n" +
                                         "      * EXPMSG - {1}";
        public static string EXPMSG003 = "      * EXPMSG - {0} not found *****";
        public static string EXPMSG004 = "      * EXPMSG - {0} loaded from: {1}";
        public static string EXPMSG005 = "      * EXPMSG - {0} detected.  Swithing to UCOB.";
        public static string EXPMSG006 = "      * EXPMSG - File-{0} -> {1}";



        public static string WORKING_STORAGE_SECTION = "WORKING-STORAGE SECTION";
        public static string PROCEDURE_DIVISION      = "PROCEDURE DIVISION";
        public static string IDENTIFICATION_DIVISION = "IDENTIFICATION DIVISION";
        public static string PROC = "PROC";
        public static string END_STMT = "END";
        public static string SC_UCOB  = "UCOB";
        public static string SC_BOTH  = "BOTH";
        public static string TEST = "TEST";
    }
}
