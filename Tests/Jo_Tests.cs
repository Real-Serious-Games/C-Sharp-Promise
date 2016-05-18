using Moq;
using RSG;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests {

    public class Jo_Tests {

        [Fact]
        public void jo_can_resolve_generic_1() {

            var promisedValue = 5;
            var promise = Promise<int>.Resolved(promisedValue);

            var completed = 0;
            promise.Then(
                value => {
                    Assert.Equal(promisedValue, value);
                    ++completed;
                    return "next";
                }
            );
            Assert.Equal(1, completed);
        }

        [Fact]
        public void jo_can_resolve_generic_2() {

            var promisedValue = 5;
            var promise = Promise<int>.Resolved(promisedValue);

            var completed = 0;
            promise.Then(
                value => {
                    Assert.Equal(promisedValue, value);
                    ++completed;
                }
            );
            Assert.Equal(1, completed);
        }

        [Fact]
        public void jo_can_resolve_generic_3() {
            
            var firstValue = 5;
            var secondValue = "two";
            var completed = 0;

            var promise = Promise<int>.Resolved(firstValue);
            var chainedPromise = Promise<string>.Resolved(secondValue);

            promise.Then(
                value => {
                    Assert.Equal(firstValue, value);
                    ++completed;
                    return chainedPromise;
                }
            ).Then(
                result => {
                    Assert.Equal(secondValue, result);
                    ++completed;
                }
            );

            Assert.Equal(2, completed);
        }

        [Fact]
        public void jo_can_resolve_generic_4() {

            var firstValue = 5;
            var promise = Promise<int>.Resolved(firstValue);
            var chainedPromise = Promise.Resolved();

            var completed = 0;
            promise.Then(
                value => {
                    Assert.Equal(firstValue, value);
                    ++completed;
                    return chainedPromise;
                }
            ).Then(
                () => {
                    ++completed;
                    
                }
            );
            Assert.Equal(2, completed);
        }

        [Fact]
        public void jo_can_reject_generic_1() {

            var ex = new Exception();
            var promise = Promise<int>.Rejected(ex);

            var completed = 0;
            promise.Then(
                value => "next",
                reason => {
                    Assert.Equal(reason, ex);
                    ++completed;
                    return "ok";
                }
            );
            Assert.Equal(1, completed);
        }

        [Fact]
        public void jo_can_reject_generic_2() {

            var ex = new Exception();
            var promise = Promise<int>.Rejected(ex);

            var completed = 0;
            promise.Then(
                value => { },
                reason => {
                    Assert.Equal(reason, ex);
                    ++completed;
                }
            );
            Assert.Equal(1, completed);
        }

        [Fact]
        public void jo_can_reject_generic_3() {

            var ex = new Exception();
            var secondValue = "two";
            var completed = 0;

            var promise = Promise<int>.Rejected(ex);
            var chainedPromise = Promise<string>.Resolved(secondValue);

            promise.Then(
				value => chainedPromise,
				reason => {
					Assert.Equal(reason, ex);
					++completed;
					return secondValue;
				}
			).Then(
                result => {
                    Assert.Equal(secondValue, result);
                    ++completed;
                }
            );

            Assert.Equal(2, completed);
        }

        [Fact]
        public void jo_can_reject_generic_4() {

            var ex = new Exception();
            var promise = Promise<int>.Rejected(ex);
            var chainedPromise = Promise.Resolved();

            var completed = 0;
            promise.Then(
                value => chainedPromise,
                reason => {
                    Assert.Equal(reason, ex);
                    ++completed;
                }
            ).Then(
                () => {
                    ++completed;
                }
            );
            Assert.Equal(2, completed);
        }
    }
}
