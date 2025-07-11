//using FluentResults;

//namespace Baubit.Traceability
//{
//    public class State<T> where T : Enum
//    {
//        private T _currentState;
//        private IReason _reason;
//        private State<T> _previousState;
//        private State<T> _nextState;

//        private ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

//        public State(T initialState)
//        {
//            _currentState = initialState;
//        }

//        public Result SetNext(T state,
//                              IReason reason = null,
//                              CancellationToken cancellationToken = default)
//        {
//            try
//            {
//                readerWriterLockSlim.EnterWriteLock();
//                if (_nextState != null) return Result.Fail("Cannot set next of an already transitioned state");
//                _previousState = new State<T>(this._currentState) { _nextState = this, _previousState = this._previousState, _reason = this._reason };
//                _currentState = state;
//                _reason = reason;
//                return Result.Ok();
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//            finally
//            {
//                readerWriterLockSlim.ExitWriteLock();
//            }
//        }

//        public Result<T> GetCurrentState()
//        {
//            try
//            {
//                readerWriterLockSlim.EnterReadLock();
//                return Result.Ok(_currentState);
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//            finally
//            {
//                readerWriterLockSlim.ExitReadLock();
//            }
//        }

//        public Result<IReason> GetReason()
//        {
//            try
//            {
//                readerWriterLockSlim.EnterReadLock();
//                return Result.Ok(_reason);
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//            finally
//            {
//                readerWriterLockSlim.ExitReadLock();
//            }
//        }

//        public Result<State<T>> GetPreviousState()
//        {
//            try
//            {
//                readerWriterLockSlim.EnterReadLock();
//                return Result.Ok(_previousState);
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//            finally
//            {
//                readerWriterLockSlim.ExitReadLock();
//            }
//        }

//        public Result<State<T>> GetNextState()
//        {
//            try
//            {
//                readerWriterLockSlim.EnterReadLock();
//                return Result.Ok(_nextState);
//            }
//            catch (Exception exp)
//            {
//                return Result.Fail(new ExceptionalError(exp));
//            }
//            finally
//            {
//                readerWriterLockSlim.ExitReadLock();
//            }
//        }
//    }
//}
