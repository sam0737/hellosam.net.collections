using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hellosam.Net.Collections.Test
{
    [TestClass]
    public class ThreadSafeObservableDictionaryTest
    {
        [TestMethod]
        public void Construction()
        {
            var d = new ThreadSafeObservableDictionary<string, string>();
        }

        // TODO: Patches Welcome =)
    }
}
