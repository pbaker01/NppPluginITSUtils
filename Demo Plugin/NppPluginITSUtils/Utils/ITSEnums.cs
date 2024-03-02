using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITS.Utils
{
    public static class ITSENums
    {
        public enum ENVIRONMENT {
            Development = 0,
            UserTest = 1,
            Pseudo = 2
        }

        public enum PROC_TYPE {
            ACOB_Proc = 0,
            UCOB_Proc = 1,
            System_Proc = 2
        }

        public enum CHG_CMMT_FORMAT {
            IIMMYY = 0,
            MMYYII = 1
        }

        public enum MAPPED_DRIVE {
            [Description("A:")] A = 0,
            [Description("B:")] B = A+1,
            [Description("C:")] C = B+1,
            [Description("D:")] D = C+1,
            [Description("E:")] E = D+1,
            [Description("F:")] F = E+1,
            [Description("G:")] G = F+1,
            [Description("H:")] H = G+1,
            [Description("I:")] I = H+1,
            [Description("J:")] J = I+1,
            [Description("K:")] K = J+1,
            [Description("L:")] L = K+1,
            [Description("M:")] M = L+1,
            [Description("N:")] N = M+1,
            [Description("O:")] O = N+1,
            [Description("P:")] P = O+1,
            [Description("Q:")] Q = P+1,
            [Description("R:")] R = Q+1,
            [Description("S:")] S = R+1,
            [Description("T:")] T = S+1,
            [Description("U:")] U = T+1,
            [Description("V:")] V = U+1,
            [Description("W:")] W = V+1,
            [Description("X:")] X = W+1,
            [Description("Y:")] Y = X+1,
            [Description("Z:")] Z = Y+1
        }

        public enum ERRORS
        {
            ITSERR001 = 1,              // This error represents ...
            ITSERR002 = ITSERR001 + 1,  // This error represents ...
            ITSERR003 = ITSERR002 + 1,  // This error represents ...
            ITSERR004 = ITSERR003 + 1,  // This error represents ...
            ITSERR005 = ITSERR004 + 1,  // This error represents ...
            ITSERR006 = ITSERR005 + 1,  // This error represents ...
            ITSERR007 = ITSERR006 + 1,  // This error represents ...
            ITSERR008 = ITSERR007 + 1,  // This error represents ...
            ITSERR009 = ITSERR008 + 1,  // This error represents ...
            ITSERR010 = ITSERR009 + 1,  // This error represents ...
            ITSERR011 = ITSERR010 + 1,  // This error represents ...
            ITSERR012 = ITSERR011 + 1,  // This error represents ...
            ITSERR013 = ITSERR012 + 1,  // This error represents ...
            ITSERR014 = ITSERR013 + 1,  // This error represents ...
            ITSERR015 = ITSERR014 + 1,  // This error represents ...
            ITSERR016 = ITSERR015 + 1,  // This error represents ...
            ITSERR017 = ITSERR016 + 1,  // This error represents ...
            ITSERR018 = ITSERR017 + 1,  // This error represents ...
            ITSERR019 = ITSERR018 + 1,  // This error represents ...
            ITSERR020 = ITSERR019 + 1   // This error represents ...
        }

    }
}
