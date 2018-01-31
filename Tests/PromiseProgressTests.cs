using System;
using Xunit;

namespace RSG.Tests
{
    public class PromiseProgressTests
    {
        [Fact]
        public void can_report_simple_progress()
        {
            var expectedStep = 0.25f;
            var currentProgress = 0f;
            var promise = new Promise();

            promise.Progress(v =>
            {
                Assert.InRange(expectedStep - (v - currentProgress), -Math.E, Math.E);
                currentProgress = v;
            });

            for (float progress = 0.25f; progress < 1f; progress += 0.25f)
                promise.ReportProgress(progress);
            promise.ReportProgress(1f);

            Assert.Equal(1f, currentProgress);
        }

        [Fact]
        public void can_handle_onProgress()
        {
            var promise = new Promise();
            var progress = 0f;

            promise.Then(null, null, v => progress = v);

            promise.ReportProgress(1f);

            Assert.Equal(1f, progress);
        }

        [Fact]
        public void can_handle_chained_onProgress()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var progressA = 0f;
            var progressB = 0f;

            promiseA
                .Then(() => promiseB, null, v => progressA = v)
                .Progress(v => progressB = v)
                .Done();

            promiseA.ReportProgress(1f);
            promiseA.Resolve();
            promiseB.ReportProgress(2f);
            promiseB.Resolve();

            Assert.Equal(1f, progressA);
            Assert.Equal(2f, progressB);
        }

        [Fact]
        public void can_do_progress_weighted_average()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var expectedSteps = new float[] { 0.1f, 0.2f, 0.6f, 1f };
            var currentProgress = 0f;
            int currentStep = 0;

            promiseC.
                Progress(v =>
                {
                    Assert.InRange(currentStep, 0, expectedSteps.Length - 1);
                    Assert.Equal(v, expectedSteps[currentStep]);
                    currentProgress = v;
                    ++currentStep;
                })
            ;

            promiseA.
                Progress(v => promiseC.ReportProgress(v * 0.2f))
                .Then(() => promiseB)
                .Progress(v => promiseC.ReportProgress(0.2f + 0.8f * v))
                .Then(() => promiseC.Resolve())
                .Catch(ex => promiseC.Reject(ex))
            ;

            promiseA.ReportProgress(0.5f);
            promiseA.ReportProgress(1f);
            promiseA.Resolve();
            promiseB.ReportProgress(0.5f);
            promiseB.ReportProgress(1f);
            promiseB.Resolve();

            Assert.Equal(1f, currentProgress);
        }


        [Fact]
        public void chain_multiple_promises_reporting_progress()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var progressA = 0f;
            var progressB = 0f;

            promiseA
                .Progress(v => progressA = v)
                .Then(() => promiseB)
                .Progress(v => progressB = v)
                .Done();

            promiseA.ReportProgress(1f);
            promiseA.Resolve();
            promiseB.ReportProgress(2f);
            promiseB.Resolve();

            Assert.Equal(1f, progressA);
            Assert.Equal(2f, progressB);
        }

        [Fact]
        public void exception_is_thrown_for_progress_after_resolve()
        {
            var promise = new Promise();
            promise.Resolve();

            Assert.Throws<ApplicationException>(() => promise.ReportProgress(1f));
        }

        [Fact]
        public void exception_is_thrown_for_progress_after_reject()
        {
            var promise = new Promise();
            promise.Reject(new ApplicationException());

            Assert.Throws<ApplicationException>(() => promise.ReportProgress(1f));
        }
    }
}
