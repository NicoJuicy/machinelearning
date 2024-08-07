﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static partial class RetryHelper
    {
        private static readonly Func<int, int> _defaultBackoffFunc = i => Math.Min(i * 100, 60_000);
        private static readonly Predicate<Exception> _defaultRetryWhenFunc = p => true;
        private static readonly bool _debug = Environment.GetEnvironmentVariable("DEBUG_RETRYHELPER") == "1";

        /// <summary>Executes the <paramref name="test"/> action up to a maximum of <paramref name="maxAttempts"/> times.</summary>
        /// <param name="maxAttempts">The maximum number of times to invoke <paramref name="test"/>.</param>
        /// <param name="test">The test to invoke.</param>
        /// <param name="backoffFunc">After a failure, invoked to determine how many milliseconds to wait before the next attempt.  It's passed the number of iterations attempted.</param>
        /// <param name="retryWhen">Invoked to select the exceptions to retry on. If not set, any exception will trigger a retry.</param>
        public static void Execute(Action test, int maxAttempts = 5, Func<int, int>? backoffFunc = null, Predicate<Exception>? retryWhen = null, [CallerMemberName] string? testName = null)
        {
            // Validate arguments
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            retryWhen ??= _defaultRetryWhenFunc;

            // Execute the test until it either passes or we run it maxAttempts times
            var exceptions = new List<Exception>();
            for (int i = 1; i <= maxAttempts; i++)
            {
                Exception lastException;
                try
                {
                    test();
                    return;
                }
                catch (Exception e) when (retryWhen(e))
                {
                    lastException = e;
                    exceptions.Add(e);
                    if (i == maxAttempts)
                    {
                        throw new AggregateException(exceptions);
                    }
                }

                if (_debug)
                {
                    string diagnostic = $"RetryHelper: retrying {testName} {i}th time of {maxAttempts}: got {lastException.Message}";
                    Console.WriteLine(diagnostic);
                    Debug.WriteLine(diagnostic);
                }

                Thread.Sleep((backoffFunc ?? _defaultBackoffFunc)(i));
            }
        }

        /// <summary>Executes the <paramref name="test"/> action up to a maximum of <paramref name="maxAttempts"/> times.</summary>
        /// <param name="maxAttempts">The maximum number of times to invoke <paramref name="test"/>.</param>
        /// <param name="test">The test to invoke.</param>
        /// <param name="backoffFunc">After a failure, invoked to determine how many milliseconds to wait before the next attempt.  It's passed the number of iterations attempted.</param>
        /// <param name="retryWhen">Invoked to select the exceptions to retry on. If not set, any exception will trigger a retry.</param>
        public static async Task ExecuteAsync(Func<Task> test, int maxAttempts = 5, Func<int, int>? backoffFunc = null, Predicate<Exception>? retryWhen = null, [CallerMemberName] string? testName = null)
        {
            // Validate arguments
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            retryWhen ??= _defaultRetryWhenFunc;

            // Execute the test until it either passes or we run it maxAttempts times
            var exceptions = new List<Exception>();
            for (int i = 1; i <= maxAttempts; i++)
            {
                try
                {
                    await test().ConfigureAwait(false);
                    return;
                }
                catch (Exception e) when (retryWhen(e))
                {
                    exceptions.Add(e);
                    if (i == maxAttempts)
                    {
                        throw new AggregateException(exceptions);
                    }
                }

                await Task.Delay((backoffFunc ?? _defaultBackoffFunc)(i)).ConfigureAwait(false);
            }
        }
    }
}
