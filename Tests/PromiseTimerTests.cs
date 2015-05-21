using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests
{
    public class PromiseTimerTests
    {
        [Fact]
        public void wait_for_doesnt_resolve_before_specified_time()
        {
            var testObject = new PromiseTimer();

            var testTime = 2f;
            var hasResolved = false;

            testObject.WaitFor(testTime)
                .Then(() => hasResolved = true)
                .Done();

            testObject.Update(1f);

            Assert.Equal(false, hasResolved);
        }

        [Fact]
        public void wait_for_resolves_after_specified_time()
        {
            var testObject = new PromiseTimer();

            var testTime = 1f;
            var hasResolved = false;

            testObject.WaitFor(testTime)
                .Then(() => hasResolved = true)
                .Done();

            testObject.Update(2f);

            Assert.Equal(true, hasResolved);
        }
    }

}
