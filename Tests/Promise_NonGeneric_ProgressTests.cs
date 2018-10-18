using System;
using RSG.Exceptions;
using Xunit;

namespace RSG.Tests
{
    public class Promise_NonGeneric_ProgressTests
    {
        [Fact]
        public void can_report_simple_progress()
        {
            const float expectedStep = 0.25f;
            var currentProgress = 0f;
            var promise = new Promise();

            promise.Progress(v =>
            {
                Assert.InRange(expectedStep - (v - currentProgress), -Math.E, Math.E);
                currentProgress = v;
            });

            for (var progress = 0.25f; progress < 1f; progress += 0.25f)
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

            var expectedProgress = new[] { 0.1f, 0.2f, 0.6f, 1f };
            var currentProgress = 0f;
            int currentStep = 0;

            promiseC.
                Progress(v =>
                {
                    Assert.InRange(currentStep, 0, expectedProgress.Length - 1);
                    Assert.Equal(v, expectedProgress[currentStep]);
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

            Assert.Equal(expectedProgress.Length, currentStep);
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

            Assert.Throws<PromiseStateException>(() => promise.ReportProgress(1f));
        }

        [Fact]
        public void exception_is_thrown_for_progress_after_reject()
        {
            var promise = new Promise();
            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() => promise.ReportProgress(1f));
        }

        [Fact]
        public void all_progress_is_averaged()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();
            var promiseD = new Promise();

            int currentStep = 0;
            var expectedProgress = new[] { 0.25f, 0.50f, 0.75f, 1f };

            Promise.All(promiseA, promiseB, promiseC, promiseD)
                .Progress(progress =>
                {
                    Assert.InRange(currentStep, 0, expectedProgress.Length - 1);
                    Assert.Equal(expectedProgress[currentStep], progress);
                    ++currentStep;
                });

            promiseA.ReportProgress(1f);
            promiseC.ReportProgress(1f);
            promiseB.ReportProgress(1f);
            promiseD.ReportProgress(1f);

            Assert.Equal(expectedProgress.Length, currentStep);
            Assert.Equal(expectedProgress.Length, currentStep);
        }

        [Fact]
        public void race_progress_is_maxed()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            int reportCount = 0;

            Promise.Race(promiseA, promiseB)
                .Progress(progress =>
                {
                    Assert.Equal(progress, 0.5f);
                    ++reportCount;
                });

            promiseA.ReportProgress(0.5f);
            promiseB.ReportProgress(0.1f);
            promiseB.ReportProgress(0.2f);
            promiseB.ReportProgress(0.3f);
            promiseB.ReportProgress(0.4f);
            promiseB.ReportProgress(0.5f);

            Assert.Equal(6, reportCount);
        }

        [Fact]
        public void all_progress_with_resolved()
        {
            var promiseA = new Promise();
            var promiseB = Promise.Resolved();
            int reportedCount = 0;

            Promise.All(promiseA, promiseB)
                .Progress(progress =>
                {
                    ++reportedCount;
                    Assert.Equal(0.75f, progress);
                });

            promiseA.ReportProgress(0.5f);

            Assert.Equal(1, reportedCount);
        }

        [Fact]
        public void sequence_reports_progress()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = Promise.Resolved();
            var promiseD = new Promise();
            int currentReport = 0;
            var expectedProgress = new[] { 0.125f, 0.25f, 0.25f, 0.3125f, 0.375f, 0.4375f, 0.5f, 0.75f, 0.875f, 1f };

            Promise
                .Sequence(() => promiseA, () => promiseB, () => promiseC, () => promiseD)
                .Progress(v =>
                {
                    Assert.Equal(expectedProgress[currentReport], v);
                    ++currentReport;
                })
                .Done()
            ;

            promiseA.ReportProgress(0.5f);
            promiseA.ReportProgress(1f);
            promiseA.Resolve();

            promiseB.ReportProgress(0.25f);
            promiseB.ReportProgress(0.5f);
            promiseB.ReportProgress(0.75f);
            promiseB.Resolve();

            promiseD.ReportProgress(0.5f);
            promiseD.ReportProgress(1f);
            promiseD.Resolve();

            Assert.Equal(expectedProgress.Length, currentReport);
        }
    }
}
