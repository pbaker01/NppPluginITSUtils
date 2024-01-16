using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITS.Utils
{
    public class ITSENums
    {
        public enum ENVIRONMENT
        {
            Development = 0,
            UserTest = 1,
            Pseudo = 2
        }

        public enum MAPPED_DRIVE
        {
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

    }
}
