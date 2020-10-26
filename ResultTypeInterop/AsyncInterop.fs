namespace ResultTypeInterop

open System
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks

module private AsyncInterop =
    let asTask(async: Async<'T>, token: CancellationToken option) =
        let tcs = TaskCompletionSource<'T>()
        let token = defaultArg token (CancellationToken());
        Async.StartWithContinuations(async,
                tcs.SetResult, tcs.SetException,
                tcs.SetException, token)
        tcs.Task
 
    let asAsync(task: Task, token: CancellationToken option) =
       Async.FromContinuations( 
                fun (completed, caught, canceled) ->
       let token = defaultArg token (CancellationToken())
       task.ContinueWith(Action<Task>(fun _ ->
                      if task.IsFaulted then caught(task.Exception)
                      else if task.IsCanceled then 
                         canceled(OperationCanceledException(token)|>raise)
                      else completed()), token)
                        |> ignore)

    let asAsyncT(task: Task<'T>, token: CancellationToken option) =
        Async.FromContinuations( 
                 fun (completed, caught, canceled) ->
        let token = defaultArg token (CancellationToken()) 
        task.ContinueWith(Action<Task<'T>>(fun _ -> 
                        if task.IsFaulted then caught(task.Exception)
                        else if task.IsCanceled then  
                            canceled(OperationCanceledException(token) |> raise)
                        else completed(task.Result)), token) 
                        |> ignore)

[<Extension>]  
type AsyncInteropExtensions =
    [<Extension>]
    static member AsAsync (task: Task<'T>) = AsyncInterop.asAsyncT (task, None) 

    [<Extension>]
    static member AsAsync (task: Task<'T>, token: CancellationToken) =
        AsyncInterop.asAsyncT (task, Some token) 

    [<Extension>]
    static member AsTask (async: Async<'T>) = AsyncInterop.asTask (async, None)

    [<Extension>]
    static member AsTask (async: Async<'T>, token: CancellationToken) =
        AsyncInterop.asTask (async, Some token) 
