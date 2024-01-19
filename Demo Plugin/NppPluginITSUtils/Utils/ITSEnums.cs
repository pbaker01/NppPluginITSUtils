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


        public enum MAPPED_DRIVE {
            [Description("A:")] A = 0,
            B = A+1,
            C = B+1,
            D = C+1,
            E = D+1,
            F = E+1,
            G = F+1,
            H = G+1,
            I = H+1,
            J = I+1,
            K = J+1,
            L = K+1,
            M = L+1,
            N = M+1,
            O = N+1,
            P = O+1,
            Q = P+1,
            R = Q+1,
            S = R+1,
            T = S+1,
            U = T+1,
            V = U+1,
            W = V+1,
            X = W+1,
            Y = X+1,
            Z = Y+1
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
