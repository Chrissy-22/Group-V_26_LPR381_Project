using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Group_V_26_LPR381_Project.Algorithms;

namespace Group_V_26_LPR381_Project.Algorithms
{
    public interface ISolver
    {
        Models.Solution Solve(Models.LinearProgram program);
    }
}
