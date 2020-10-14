#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.FSharp

type Gossip =
    | Initailize of IActorRef []
    | StartGossip of String
    | ReportMsgRecvd of String
    | StartPushSum of Double
    | ComputePushSum of Double * Double * Double
    | Result of Double * Double
    | Time of int
    | TotalNodes of int
    | Shutdown of int*IActorRef []
    | GetNeighborsToIgnore of int []
type Topology = 
    | Gossip of String
    | PushSum of String

type Protocol = 
    | Line of String
    | Full of String
    | TwoDimension of String

    
//https://gist.github.com/akimboyko/e58e4bfbba3e9a551f05 Example 3

type Supervisor()  =
    inherit Actor()
    let mutable count = 0
    let mutable start = 0
    let mutable totalNodes = 0

    override x.OnReceive(msg) =
        match msg :?> Gossip with
        | ReportMsgRecvd _ ->
            let ending = DateTime.Now.TimeOfDay.Milliseconds

            count <- count + 1
            if count = totalNodes then
                printfn "Time for convergence: %i ms" (ending - start)
                Environment.Exit(0)
       
           
        | Result (sum, weight) ->
            let ending = DateTime.Now.TimeOfDay.Milliseconds
            printfn "Sum = %f Weight= %f Average=%f" sum weight (sum / weight)
            printfn "Time for convergence: %i ms" (ending - start)
            Environment.Exit(0)
        | Time strtTime -> start <- strtTime

        | TotalNodes n -> totalNodes <- n
        | _ -> ()

type Worker(supervisor: IActorRef, numResend: int, nodeNum: int) =
    inherit Actor()
    let mutable rumourCount = 0
    let mutable neighbours: IActorRef [] = [||]
    let mutable neighborsToIgoreArray:int [] = [||]
    //used for push sum
    let mutable sum = nodeNum |> float
    let mutable weight = 1.0
    let mutable termRound = 1

    override x.OnReceive(num) =
        match num :?> Gossip with
        | Initailize aref -> neighbours <- aref
        | GetNeighborsToIgnore shutdownNeighborsList -> neighborsToIgoreArray <-shutdownNeighborsList
        | StartGossip msg ->
            rumourCount <- rumourCount + 1
            if (rumourCount = 10) then supervisor <! ReportMsgRecvd(msg)
            if (rumourCount <= 100) then
                let mutable rnd = Random().Next(0, neighbours.Length)
                while Array.contains rnd neighborsToIgoreArray do
                    // printfn "Server not available Server%i " rnd 
                    rnd <- Random().Next(0, neighbours.Length)
                // printf "gossip for %i" rnd 
                neighbours.[rnd] <! StartGossip(msg)

        | StartPushSum delta ->
            let index = Random().Next(0, neighbours.Length)

            sum <- sum / 2.0
            weight <- weight / 2.0
            neighbours.[index] <! ComputePushSum(sum, weight, delta)

        | ComputePushSum (s: float, w, delta) ->
            let newsum = sum + s
            let newweight = weight + w

            let cal =
                sum / weight - newsum / newweight |> abs


            match (s, w, delta) with
            | (_, _, delta) when cal > delta -> 
                termRound <- 0
                sum <- sum + s
                weight <- weight + w
                sum <- sum / 2.0
                weight <- weight / 2.0

                let index =
                    Random().Next(0, neighbours.Length)

                neighbours.[index]
                <! ComputePushSum(sum, weight, delta)
            | (_, _, _) when termRound >= 3 -> 
                supervisor <! Result(sum, weight)
            | _ ->  
                sum <- sum / 2.0
                weight <- weight / 2.0
                termRound <- termRound + 1

                let index =
                    Random().Next(0, neighbours.Length)

                neighbours.[index]
                <! ComputePushSum(sum, weight, delta)
        | _ -> ()

//Point of execution
let mutable nodes =
    int (string (fsi.CommandLineArgs.GetValue 1))

let topology = string (fsi.CommandLineArgs.GetValue 2)
let protocol = string (fsi.CommandLineArgs.GetValue 3)
let numNodesToIgnore = string (fsi.CommandLineArgs.GetValue 4) |>int


let system = ActorSystem.Create("System")

let mutable actualNumOfNodes = nodes |> float
let mutable shutdownNeighborsList : int [] = [||]


nodes =
    match topology with
    | "2D" | "imp2D" -> 
        (actualNumOfNodes ** 0.5) ** 2.0 |> float |> int
    | _ -> nodes


let supervisor =
    system.ActorOf(Props.Create(typeof<Supervisor>), "supervisor")

match topology with
| "line" ->
    let nodeArray = Array.zeroCreate (nodes + 1)
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <- system.ActorOf(Props.Create(typeof<Worker>, supervisor, 10, i + 1), "demo" + string (i)))
        
    for i in [ 0 .. nodes ] do
        printfn "%i %i %i" ((i-1+nodes) % (nodes)) i ((i+1+nodes) % (nodes))
        let mutable neighbourArray = [||]

        
        if i = 0 then
            neighbourArray <- (Array.append neighbourArray [|nodeArray.[i+1]|])
        elif i = nodes then
            neighbourArray <- (Array.append neighbourArray [|nodeArray.[i-1]|])
        else 
            neighbourArray <- (Array.append neighbourArray [| nodeArray.[(i - 1)] ; nodeArray.[(i + 1 ) ] |] ) 
        
        nodeArray.[i] <! Initailize(neighbourArray)
        
    for i = 1 to numNodesToIgnore do   
        let mutable rnd_shutdown = Random().Next(0, nodes)
        printfn  "shutting down %i"  rnd_shutdown
        shutdownNeighborsList <- (Array.append shutdownNeighborsList [|rnd_shutdown|])
        
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <! GetNeighborsToIgnore(shutdownNeighborsList))

    let leader = Random().Next(0, nodes)
    
   
    match protocol with
    | "gossip" -> 
        supervisor <! TotalNodes(nodes)
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "Starting Protocol Gossip"
        nodeArray.[leader] <! StartGossip("This is Line Topology")
    | "push-sum" -> 
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "Starting Push Sum Protocol for Line"
        nodeArray.[leader] <! StartPushSum(10.0 ** -10.0)     
    | _ ->
        printfn "Invlaid topology"   

| "full" ->
    let nodeArray = Array.zeroCreate (nodes + 1)
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <- system.ActorOf(Props.Create(typeof<Worker>, supervisor, 10, i + 1), "demo" + string (i)))
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <! Initailize(nodeArray))

    for i = 1 to numNodesToIgnore do   
        let mutable rnd_shutdown = Random().Next(0, nodes)
        let nodeToShutdown = nodeArray.[rnd_shutdown]
        printfn  "shutting down %i"  rnd_shutdown
        shutdownNeighborsList <- (Array.append shutdownNeighborsList [|rnd_shutdown|])
    
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <! GetNeighborsToIgnore(shutdownNeighborsList))
    let leader = Random().Next(0, nodes)

    match protocol with
    | "gossip" -> 
        supervisor <! TotalNodes(nodes - numNodesToIgnore)
        
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "------------- Begin Gossip -------------"
        nodeArray.[leader] <! StartGossip("Hello")
    | "push-sum" -> 
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "------------- Begin Push Sum -------------"
        nodeArray.[leader] <! StartPushSum(10.0 ** -10.0)
    | _ ->
        printfn "Invalid topology"

| "2D" ->
    let gridSize = actualNumOfNodes |> sqrt |> ceil |> int 

    let totGrid = gridSize * gridSize
    let nodeArray = Array.zeroCreate (totGrid)
    
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <- system.ActorOf(Props.Create(typeof<Worker>, supervisor, 10, i + 1), "demo" + string (i)))

    for i in [ 0 .. gridSize - 1 ] do
        for j in [ 0 .. gridSize - 1 ] do
            let mutable neighbours: IActorRef [] = [||]
            match (i, j) with
            | (_, j) when j + 1 < gridSize -> neighbours <- (Array.append neighbours [| nodeArray.[i * gridSize + j + 1] |])
            | (_, j) when j - 1 >= 0 -> neighbours <- (Array.append neighbours [| nodeArray.[i * gridSize + j + 1] |])
            | (i, _) when i - 1 >= 0 -> neighbours <- Array.append neighbours [| nodeArray.[i * gridSize + j - 1] |]
            | (i, _) when i + 1 < gridSize -> neighbours <- (Array.append neighbours [| nodeArray.[(i + 1) * gridSize + j] |])
                        
            nodeArray.[i * gridSize + j] <! Initailize(neighbours)

    for i = 1 to numNodesToIgnore do   
        printfn "%i" i
        let mutable rnd_shutdown = Random().Next(0, nodes)
        let nodeToShutdown = nodeArray.[rnd_shutdown]
        printfn  "shutting down %i"  rnd_shutdown
        shutdownNeighborsList <- (Array.append shutdownNeighborsList [|rnd_shutdown|])
    
    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <! GetNeighborsToIgnore(shutdownNeighborsList))


    let leader = Random().Next(0, totGrid - 1)
    match protocol with 
    | "gossip" -> 
        supervisor <! TotalNodes(totGrid - 1)
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "------------- Begin Gossip -------------"
        nodeArray.[leader] <! StartGossip("This is 2D Topology")
    | "push-sum" -> 
        supervisor <! Time(DateTime.Now.TimeOfDay.Milliseconds)
        printfn "------------- Begin Push Sum -------------"
        nodeArray.[leader] <! StartPushSum(10.0 ** -10.0)
    | _ -> 
        printfn "Invalid topology"
| _ -> ()

Console.ReadLine() |> ignore
    