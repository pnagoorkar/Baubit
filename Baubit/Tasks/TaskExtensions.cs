﻿using FluentResults;

namespace Baubit.Tasks
{
    public static class TaskExtensions
    {
        public static Result Wait(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                task.Wait();
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }
        public static async Task<Result> WaitAsync(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                await task;
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }
    }
}
