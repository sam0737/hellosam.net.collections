using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hellosam.Net.Collections.Test
{
    [TestClass]
    public class ConcurrentObservableDictionaryTest
    {
        [TestMethod]
        public void Construction()
        {
            var d = new ConcurrentObservableDictionary<string, string>();
        }
    }
}
