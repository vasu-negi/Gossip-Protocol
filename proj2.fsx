#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Diagnostics
    


type Gossip =
    | Initailize of IActorRef []
    | InitializeVariables of int
    | StartGossip of String
    | ReportMsgRecvd of String
    | StartPushSum of Double
    | ComputePushSum of Double * Double * Double
    | Result of Double * Double
    | Time of int
    | TotalNodes of int

type Topology = 
    | Gossip of String
    | PushSum of String

type Protocol = 
    | Line of String
    | Full of String
    | TwoDimension of String


//Point of execution
let mutable nodes =
    int (string (fsi.CommandLineArgs.GetValue 1))

let topology = string (fsi.CommandLineArgs.GetValue 2)
let protocol = string (fsi.CommandLineArgs.GetValue 3)



let system = ActorSystem.Create("System")

let mutable actualNumOfNodes = nodes |> float


nodes =
    match topology with
    | "2D" | "imp2D" -> 
        (actualNumOfNodes ** 0.5) ** 2.0 |> float |> int
    | _ -> nodes




let Supervisor(mailbox: Actor<_>) =
    
    let mutable count = 0
    let mutable start = 0
    let mutable totalNodes = 0

    let rec loop()= actor{
        let! msg = mailbox.Receive();
        match msg with 
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

        return! loop()
    }            
    loop()



let supervisor = spawn system "Supervisor" Supervisor

let Worker(mailbox: Actor<_>) =
    let mutable rumourCount = 0
    let mutable neighbours: IActorRef [] = [||]

    
    let mutable sum = 0 |>float
    let mutable weight = 1.0
    let mutable termRound = 1


    
    let rec loop()= actor{
        let! message = mailbox.Receive();
        match message with 
        

        | Initailize aref -> neighbours <- aref

        | StartGossip msg ->
            rumourCount <- rumourCount + 1
            
            if (rumourCount = 10) then supervisor <! ReportMsgRecvd(msg)

            if (rumourCount <= 100) then
                let rnd = Random().Next(0, neighbours.Length)
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

        return! loop()
    }            
    loop()






match topology with
| "line" ->
    let nodeArray = Array.zeroCreate (nodes + 1)
    //[0..nodes] |> List.iter (fun i -> nodeArray.[i] <- system.ActorOf(Props.Create(Worker, supervisor, 10, i + 1), "demo" + string (i)))

    for x in [0..nodes] do
        let key: string = "demo" + string(x) 
        let actorRef = spawn system (key) Worker
        nodeArray.[x] <- actorRef 
        
    

    for i in [ 0 .. nodes ] do
        let mutable neighbourArray = [||]
        if i = 0 then
            neighbourArray <- (Array.append neighbourArray [|nodeArray.[i+1]|])
        elif i = nodes then
            neighbourArray <- (Array.append neighbourArray [|nodeArray.[i-1]|])
        else 
            neighbourArray <- (Array.append neighbourArray [| nodeArray.[(i - 1)] ; nodeArray.[(i + 1 ) ] |] ) 
        
        nodeArray.[i] <! Initailize(neighbourArray)


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
    

    for x in [0..nodes] do
        let key: string = "demo" + string(x) 
        let actorRef = spawn system (key) Worker
        nodeArray.[x] <- actorRef 
        


    [0..nodes] |> List.iter (fun i -> nodeArray.[i] <! Initailize(nodeArray))

    let leader = Random().Next(0, nodes)

    match protocol with
    | "gossip" -> 
        supervisor <! TotalNodes(nodes)
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
    //[0..nodes] |> List.iter (fun i -> nodeArray.[i] <- system.ActorOf(Props.Create(typeof<Worker>, supervisor, 10, i + 1), "demo" + string (i)))
    for x in [0..nodes] do
        let key: string = "demo" + string(x) 
        let actorRef = spawn system (key) Worker
        nodeArray.[x] <- actorRef 
        

    for i in [ 0 .. gridSize - 1 ] do
        for j in [ 0 .. gridSize - 1 ] do
            let mutable neighbours: IActorRef [] = [||]
            match (i, j) with
            | (_, j) when j + 1 < gridSize -> neighbours <- (Array.append neighbours [| nodeArray.[i * gridSize + j + 1] |])
            | (_, j) when j - 1 >= 0 -> neighbours <- (Array.append neighbours [| nodeArray.[i * gridSize + j + 1] |])
            | (i, _) when i - 1 >= 0 -> neighbours <- Array.append neighbours [| nodeArray.[i * gridSize + j - 1] |]
            | (i, _) when i + 1 < gridSize -> neighbours <- (Array.append neighbours [| nodeArray.[(i + 1) * gridSize + j] |])
                        
            nodeArray.[i * gridSize + j] <! Initailize(neighbours)



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
    