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
            var promiseTimer = new PromiseTimer();
            var testTime = 2f;
            var hasResolved = false;

            promiseTimer.WaitFor(testTime)
                .Then(() => hasResolved = true)
                .Done();

            promiseTimer.Update(1f);

            Assert.Equal(false, hasResolved);
        }
    }
}
