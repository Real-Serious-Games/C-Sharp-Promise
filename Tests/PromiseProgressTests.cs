using System;
using RSG.Exceptions;
using Xunit;

namespace RSG.Tests
{
    public class PromiseProgressTests
    {
        [Fact]
        public void can_report_simple_progress()
        {
            const float expectedStep = 0.25f;
            var currentProgress = 0f;
            var promise = new Promise<int>();

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
            var promise = new Promise<int>();
            var progress = 0f;

            promise.Then(null, null, v => progress = v);

            promise.ReportProgress(1f);

            Assert.Equal(1f, progress);
        }

        [Fact]
        public void can_handle_chained_onProgress()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var progressA = 0f;
            var progressB = 0f;
            int result = 0;

            promiseA
                .Then(v => promiseB, null, v => progressA = v)
                .Progress(v => progressB = v)
                .Then(v => result = v)
                .Done();

            promiseA.ReportProgress(1f);
            promiseA.Resolve(-17);
            promiseB.ReportProgress(2f);
            promiseB.Resolve(17);

            Assert.Equal(1f, progressA);
            Assert.Equal(2f, progressB);
            Assert.Equal(17, result);
        }

        [Fact]
        public void can_do_progress_weighted_average()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<int>();

            var expectedProgress = new[] { 0.1f, 0.2f, 0.6f, 1f };
            var currentProgress = 0f;
            int currentStep = 0;
            int result = 0;

            promiseC.
                Progress(v =>
                {
                    Assert.InRange(currentStep, 0, expectedProgress.Length - 1);
                    Assert.Equal(v, expectedProgress[currentStep]);
                    currentProgress = v;
                    ++currentStep;
                })
                .Then(v => result = v)
                .Done()
            ;

            promiseA.
                Progress(v => promiseC.ReportProgress(v * 0.2f))
                .Then(v => promiseB, null)
                .Progress(v => promiseC.ReportProgress(0.2f + 0.8f * v))
                .Then(v => promiseC.Resolve(v))
                .Catch(ex => promiseC.Reject(ex))
            ;

            promiseA.ReportProgress(0.5f);
            promiseA.ReportProgress(1f);
            promiseA.Resolve(-17);
            promiseB.ReportProgress(0.5f);
            promiseB.ReportProgress(1f);
            promiseB.Resolve(17);

            Assert.Equal(expectedProgress.Length, currentStep);
            Assert.Equal(1f, currentProgress);
            Assert.Equal(17, result);
        }


        [Fact]
        public void chain_multiple_promises_reporting_progress()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var progressA = 0f;
            var progressB = 0f;
            int result = 0;

            promiseA
                .Progress(v => progressA = v)
                .Then(v => promiseB, null)
                .Progress(v => progressB = v)
                .Then(v => result = v)
                .Done()
            ;

            promiseA.ReportProgress(1f);
            promiseA.Resolve(-17);
            promiseB.ReportProgress(2f);
            promiseB.Resolve(17);

            Assert.Equal(1f, progressA);
            Assert.Equal(2f, progressB);
            Assert.Equal(17, result);
        }

        [Fact]
        public void exception_is_thrown_for_progress_after_resolve()
        {
            var promise = new Promise<int>();
            promise.Resolve(17);

            Assert.Throws<PromiseStateException>(() => promise.ReportProgress(1f));
        }

        [Fact]
        public void exception_is_thrown_for_progress_after_reject()
        {
            var promise = new Promise<int>();
            promise.Reject(new Exception());

            Assert.Throws<PromiseStateException>(() => promise.ReportProgress(1f));
        }

        [Fact]
        public void first_progress_is_averaged()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<int>();
            var promiseD = new Promise<int>();

            int currentStep = 0;
            var expectedProgress = new[] { 0.25f, 0.50f, 0.75f, 1f };

            Promise<int>.First(() => promiseA, () => promiseB, () => promiseC, () => promiseD)
                .Progress(progress =>
                {
                    Assert.InRange(currentStep, 0, expectedProgress.Length - 1);
                    Assert.Equal(expectedProgress[currentStep], progress);
                    ++currentStep;
                });

            promiseA.Reject(null);
            promiseC.Reject(null);
            promiseB.Reject(null);
            promiseD.Reject(null);

            Assert.Equal(expectedProgress.Length, currentStep);
        }

        [Fact]
        public void all_progress_is_averaged()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<int>();
            var promiseD = new Promise<int>();

            int currentStep = 0;
            var expectedProgress = new[] { 0.25f, 0.50f, 0.75f, 1f };

            Promise<int>.All(promiseA, promiseB, promiseC, promiseD)
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
        }

        [Fact]
        public void race_progress_is_maxed()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            int reportCount = 0;

            Promise<int>.Race(promiseA, promiseB)
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
            var promiseA = new Promise<int>();
            var promiseB = Promise<int>.Resolved(17);
            int reportedCount = 0;

            Promise<int>.All(promiseA, promiseB)
                .Progress(progress =>
                {
                    ++reportedCount;
                    Assert.Equal(0.75f, progress);
                });

            promiseA.ReportProgress(0.5f);

            Assert.Equal(1, reportedCount);
        }
    }
}
