//namespace BioFSharp.Stats

//open System
//open System.IO

//open FSharpAux
//open FSharpAux.IO
//open FSharp.Stats
//open FSharp.Plotly

//open FSharpGephiStreamer

//module SignatureTopology =

//    /// Item and Tree type
//    module Types =

//        /// basic type for data (after processing)
//        type Item = {
//            ID          : int
//            ProteinL    : string array
//            OriginalBin : string array
//            BinL        : string array 
//            dataL       : float array
//            }

//        /// Generic tree node containing member list and child map
//        type Node<'key, 'data when 'key : comparison> = 
//            {
//            MaxGain  : float                    // V, Value Function for the node: max from stepGain and confGain
//            StepGain : float                    // S, Step Gain for the node 
//            ConfGain : float * (string list)    // C, Configuration Gain for the node; the keys of best children configuration
//            Members  : 'data array
//            Children : Map<'key, Node<'key,'data>> 
//            }

//        /// Clustering function, examples are in Main.Clustering
//        type ClusterFn = Map<string, Item []> -> int list -> (string * (Item [])) [] [] list

//        /// Mapping for preclustering, example see in TestData.ChlamyTranscriptomeHermit
//        type PreclusterShapeMap = Map<string,int>

//        /// State Space Search Walk Function, applied function is in Main.Walk
//        type WalkingFn = ClusterFn -> int -> float [,] -> Map<string,Node<string,Item>> -> (float -> float -> int -> int -> float) -> Map<string, Item []> -> Map<string, Item []>

//        /// creating/alterating tree function mode
//        type Mode =   
//            |MM_orig                                            // original MapMan only annotated subbins
//            |MM                                                 // preprocessed MapMan (with singleton leaves, without tunnels)
//            |ST of (PreclusterShapeMap * ClusterFn * WalkingFn) // ST with preclustering and State Space Search Walk
//            |ST_walk of (ClusterFn * WalkingFn)                 // ST without preclustering, with only State Space Search Walk
//            |ST_combi                                           // ST without simplification, pure combinatorics

//    /// Priority Queue structure type. Used for SSSW algorithm
//    module PQ =

//        /// Create binary tree with MAX on top
//        type MaxIndexPriorityQueue<'T when 'T : comparison>(n:int) = 

//            let objects : 'T []      = Array.zeroCreate n //ResizeArray<'T>(n)
//            let heap    : int []     = Array.zeroCreate (n+1) //ResizeArray<int>(n)
//            let heapInverse : int [] = Array.zeroCreate n//ResizeArray<int>(n)
//            let mutable m_count      = 0

//            let swapInplace i j =
//                // swap elements in heap
//                let tmp = heap.[i]
//                heap.[i] <- heap.[j]
//                heap.[j] <- tmp

//                // reset inverses
//                heapInverse.[heap.[i]] <- i
//                heapInverse.[heap.[j]] <- j

//            let parent heapIndex = heapIndex / 2
//            let firstChild heapIndex = heapIndex * 2 
//            let secondChild heapIndex = heapIndex * 2 + 1

//            let sortHeapDownward heapIndex = // we are looking if the object is smaller then its children 
//                let rec loop heapIndex =
//                    let childIndex = firstChild heapIndex
//                    if (childIndex <= m_count) then
//                        let child = // choose the biggest of children, if there both availbale 
//                            if (childIndex < m_count) && ( objects.[heap.[childIndex + 1]] > objects.[heap.[childIndex]] ) then // change here
//                                childIndex+1 
//                            else 
//                                childIndex
//                        // swap with child if the child is bigger
//                        if ( objects.[heap.[child]] > objects.[heap.[heapIndex]] ) then // change here
//                            swapInplace child heapIndex
//                            loop child
//                loop heapIndex

//            let sortHeapUpward heapIndex = // check if the object bigger than its parent, if yes, move it upper
//                let rec loop heapIndex =
//                    let parentIndex = parent( heapIndex )
//                    if (heapIndex > 1 && 
//                            objects.[heap.[heapIndex]] > objects.[heap.[parentIndex]] ) then // change here 
//                        // swap this node with its parent
//                        swapInplace heapIndex parentIndex
//                        // reset iterator to be at parents old position
//                        // (child's new position)
//                        loop parentIndex
//                loop heapIndex

//            let sortUpward realIndex   = sortHeapUpward   heapInverse.[realIndex]
//            let sortDownward realIndex = sortHeapDownward heapInverse.[realIndex]
    
//            /// Clear the counter, get ready to refill the queue
//            member this.Clear() = m_count <- 0

//            /// Increase the value at the current index
//            member this.IncreaseValueAtIndex index (obj : 'T) =
//                if not (index < objects.Length && index >= 0) then 
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: Index %i out of range" index
//                if (obj <= objects.[index]) then // check if new value really bigger
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: object '%A' isn't greater than current value '%A'" obj objects.[index]
//                objects.[index] <- obj
//                sortUpward index

//            /// Decrease the value at the current index
//            member this.DecreaseValueAtIndex index (obj:'T) =
//                if not (index < objects.Length && index >= 0) then 
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: Index %i out of range" index
//                if (obj >= objects.[index]) then // check if new value really smaller
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: object '%A' isn't less than current value '%A'" obj objects.[index]
//                objects.[index] <- obj
//                sortDownward index

//            /// Updates the value at the given index. Note that this function is not
//            /// as efficient as the DecreaseIndex/IncreaseIndex methods, but is
//            /// best when the value at the index is not known
//            member this.Set index (obj:'T) =
//                if ( obj >= objects.[index] ) then // change here if change MAX MIN sorting
//                    this.IncreaseValueAtIndex index obj 
//                else 
//                    this.DecreaseValueAtIndex index obj

//            /// Removes the top element from the queue 
//            member this.Pop () =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if (m_count = 0) then 
//                    Unchecked.defaultof<'T>
//                else
//                    // swap front to back for removal
//                    swapInplace 1 (m_count)
//                    m_count <- m_count - 1
//                    // re-sort heap
//                    sortHeapDownward 1
//                    // return popped object
//                    //m_objects[m_heap[m_count + 1]]
//                    objects.[heap.[m_count+1]]

//            /// Removes the element with given index from the queue 
//            member this.Remove index =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if ((heapInverse.[index]) > m_count) then 
//                    failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                else
//                    if heapInverse.[index]=m_count then
//                        m_count <- m_count - 1
//                    else
//                        let heapIndex = heapInverse.[index]
//                        // swap front to back for removal
//                        swapInplace heapInverse.[index] (m_count)
//                        m_count <- m_count - 1
//                        // re-sort heap
//                        sortHeapDownward heapIndex

//            /// Removes the element with given index from the queue 
//            member this.TryRemove index =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if ((heapInverse.[index]) <= m_count) then 
//                    if heapInverse.[index]=m_count then
//                        m_count <- m_count - 1
//                    else
//                        let heapIndex = heapInverse.[index]
//                        // swap front to back for removal
//                        swapInplace heapInverse.[index] (m_count)
//                        m_count <- m_count - 1
//                        // re-sort heap
//                        sortHeapDownward heapIndex
    
//            /// Put indexOut element outside of the heap and move indexIn element inside the heap
//            member this.Swap indexOut indexIn =
//                if ((heapInverse.[indexOut]) <= m_count) then 
//                    failwith "IndexedPriorityQueue.Return: The element was already removed from heap"
//                elif ((heapInverse.[indexIn]) > m_count) then 
//                    failwith "IndexedPriorityQueue.Return: The element was already inside heap"  
//                else   
//                    swapInplace (heapInverse.[indexOut]) (heapInverse.[indexIn])
//                    if ( objects.[indexIn] > objects.[indexOut] ) then // change here if change MAX MIN sorting
//                        sortUpward indexOut
//                    else 
//                        sortDownward indexOut

//            /// Removes the element with given index from the queue 
//            member this.RemoveGroup (indeces: int []) =
//                indeces
//                    |> Array.iter (fun id -> 
//                        let c = heapInverse.[id]                
//                        if (c > m_count) then 
//                            failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                        elif c=m_count then           // check if the element is the last in the heap
//                            m_count <- m_count - 1  // remove the last element out of the heap
//                        else 
//                            swapInplace c (m_count) // swap with the last element from the heap
//                            m_count <- m_count - 1  // remove the now last element out of the heap
//                            // re-sort heap
//                            sortHeapDownward c )
        
//            /// Removes the element with given index from the queue 
//            member this.TryRemoveGroup (indeces: int []) =
//                    indeces
//                    |> Array.iter (fun id -> 
//                        let c = heapInverse.[id]                
//                        if c=m_count then           // check if the element is the last in the heap
//                            m_count <- m_count - 1  // remove the last element out of the heap
//                        elif (c<m_count) && (c>0) then
//                            swapInplace c (m_count) // swap with the last element from the heap
//                            m_count <- m_count - 1  // remove the now last element out of the heap
//                            // re-sort heap
//                            sortHeapDownward c )

//            member this.ReturnGroup (indeces: int[]) =
//                let heapIDs = 
//                    [|for id in indeces ->
//                        let c = heapInverse.[id]
//                        if (c <= m_count) then 
//                            failwith "IndexedPriorityQueue.ReturnGroup: The element was already inside"
//                        else
//                            // swap front to back for removal
//                            swapInplace c (m_count+1)
//                            m_count <- m_count + 1
//                            c  |] 
//                    |> Array.sort
//                // re-sort heap
//                for id in heapIDs do
//                    sortHeapUpward id

//            member this.TryReturnGroup (indeces: int[]) =
//                let heapIDs =
//                    indeces
//                    |> Array.map (fun id -> heapInverse.[id])
//                    |> Array.filter (fun c -> c > m_count)
//                    |> Array.map (fun c -> 
//                        swapInplace c (m_count+1)
//                        m_count <- m_count + 1
//                        c)
//                    |> Array.sort
//                // re-sort heap
//                for id in heapIDs do
//                    sortHeapUpward id

//            /// Removes all elements except those with given index from the queue 
//            member this.LeaveGroup (indeces: int []) =
//                let x = indeces.Length
//                indeces
//                |> Array.map (fun i -> heapInverse.[i])
//                |> Array.sort
//                |> Array.iteri (fun i c ->
//                    //if (c > m_count) then 
//                    //    failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                    //else
//                        // swap front to back for removal
//                        swapInplace c (i+1) )
//                // re-sort heap
//                m_count <- x
//                for id=2 to x do
//                    sortHeapUpward id

//            /// Gets the top element of the queue
//            member this.Top() = 
//                // top of heap [first element is 1, not 0]
//                objects.[heap.[1]]

//            /// Gets the index of the top element of the queue
//            member this.TopIndex() = 
//                // top of heap [first element is 1, not 0]
//                heap.[1]

//            /// Inserts a new value with the given index in the queue
//            member this.Insert index (value:'T) =
//                //if (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Insert: Index %i out of range" index
//                m_count <- m_count + 1
        
//                // add object
//                objects.[index] <- value

//                // add to heap
//                heapInverse.[index] <- m_count
//                heap.[m_count] <- index

//                // update heap
//                sortHeapUpward m_count // the new item is by default in the bottom of the tree, as the smallest. So check, if it is not the case, then move it up
        
//            member this.Length = 
//                m_count

//            /// Returns an item of the set with given index
//            member this.Item 
//                with get index = 
//                    if not (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                    objects.[index]
//                and  set index value = 
//                    if not (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                    this.Set index  value 
    
//            /// Returns an item of the heap with given index
//            member this.HeapItem index =
//                if (index <= 0) || (index > m_count) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                objects.[heap.[index]]

//            /// Returns an index in original objects order of the heap element with given index
//            member this.HeapItemIndex index =
//                if (index <= 0) || (index > m_count) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                heap.[index]

//            /// Set internal properties of the type directly    
//            member internal this.SetData (_objects: 'T []) (_heap:int[]) (_heapInverse:int[]) (_m_count: int) = 
//                for i=0 to (n-1) do
//                    objects.[i]       <- _objects.[i] 
//                    heap.[i]          <- _heap.[i]
//                    heapInverse.[i]   <- _heapInverse.[i]
//                heap.[objects.Length] <- _heap.[objects.Length]
//                m_count <- _m_count
    
//            /// deep Copy the variable into a new one
//            member this.DeepCopy() = 
//                let temp = new MaxIndexPriorityQueue<'T>(n)
//                temp.SetData objects heap heapInverse m_count
//                temp



//        /// Create binary tree with MAX on top
//        type MinIndexPriorityQueue<'T when 'T : comparison>(n:int) = 

//            let objects : 'T []      = Array.zeroCreate n //ResizeArray<'T>(n)
//            let heap    : int []     = Array.zeroCreate (n+1) // (heap.[1] is the peak and has an index of the minimal value as a value)
//            let heapInverse : int [] = Array.zeroCreate n//ResizeArray<int>(n) heapInverse.[index] gives a position of the object.[index] in the heap
//            let mutable m_count      = 0

//            /// swap elements in heap with heapIndeces i and j
//            let swapInplace i j =
//                // swap elements in heap
//                let tmp = heap.[i]
//                heap.[i] <- heap.[j]
//                heap.[j] <- tmp

//                // reset inverses
//                heapInverse.[heap.[i]] <- i
//                heapInverse.[heap.[j]] <- j

//            let parent heapIndex = heapIndex / 2
//            let firstChild heapIndex = heapIndex * 2 
//            let secondChild heapIndex = heapIndex * 2 + 1

//            let sortHeapDownward heapIndex = // we are looking if the object is bigger then its children 
//                let rec loop heapIndex =
//                    let childIndex = firstChild heapIndex
//                    if (childIndex <= m_count) then
//                        let child = // choose the smallest of children, if there both availbale 
//                            if (childIndex < m_count) && ( objects.[heap.[childIndex + 1]] < objects.[heap.[childIndex]] ) then // change here
//                                childIndex+1 
//                            else 
//                                childIndex
//                        // swap with child if the child is smaller
//                        if ( objects.[heap.[child]] < objects.[heap.[heapIndex]] ) then // change here
//                            swapInplace child heapIndex
//                            loop child
//                loop heapIndex

//            let sortHeapUpward heapIndex = // check if the object smaller than its parent, if yes, move it upper
//                let rec loop heapIndex =
//                    let parentIndex = parent heapIndex 
//                    if (heapIndex > 1 && 
//                            objects.[heap.[heapIndex]] < objects.[heap.[parentIndex]] ) then // change here 
//                        // swap this node with its parent
//                        swapInplace heapIndex parentIndex
//                        // reset iterator to be at parents old position
//                        // (child's new position)
//                        loop parentIndex
//                loop heapIndex

//            let sortUpward realIndex   = sortHeapUpward   heapInverse.[realIndex]
//            let sortDownward realIndex = sortHeapDownward heapInverse.[realIndex]
    
//            /// Clear the counter, get ready to refill the queue
//            member this.Clear() = m_count <- 0
    
//            /// Increase the value at the current index
//            member this.IncreaseValueAtIndex realIndex (obj : 'T) =
//                if not (realIndex < objects.Length && realIndex >= 0) then 
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: Index %i out of range" realIndex
//                if (obj <= objects.[realIndex]) then // check if new value really bigger
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: object '%A' isn't greater than current value '%A'" obj objects.[realIndex]
//                objects.[realIndex] <- obj
//                sortDownward realIndex

//            /// Decrease the value at the current index
//            member this.DecreaseValueAtIndex realIndex (obj:'T) =
//                if not (realIndex < objects.Length && realIndex >= 0) then 
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: Index %i out of range" realIndex
//                if (obj >= objects.[realIndex]) then // check if new value really smaller
//                    failwithf "IndexedPriorityQueue.DecreaseIndex: object '%A' isn't less than current value '%A'" obj objects.[realIndex]
//                objects.[realIndex] <- obj
//                sortUpward realIndex

//            /// Updates the value at the given index. Note that this function is not
//            /// as efficient as the DecreaseIndex/IncreaseIndex methods, but is
//            /// best when the value at the index is not known
//            member this.Set realIndex (obj:'T) =
//                if ( obj >= objects.[realIndex] ) then // change here if change MAX MIN sorting
//                    this.IncreaseValueAtIndex realIndex obj 
//                else 
//                    this.DecreaseValueAtIndex realIndex obj

//            /// Removes the top element from the queue 
//            member this.Pop () =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if (m_count = 0) then 
//                    Unchecked.defaultof<'T>
//                else
//                    // swap front to back for removal
//                    swapInplace 1 (m_count)
//                    m_count <- m_count - 1
//                    // re-sort heap
//                    sortHeapDownward 1
//                    // return popped object
//                    //m_objects[m_heap[m_count + 1]]
//                    objects.[heap.[m_count+1]]

//            /// Removes the element with given index from the queue 
//            member this.Remove realIndex =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if ((heapInverse.[realIndex]) > m_count) then 
//                    failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                else
//                    if heapInverse.[realIndex]=(m_count) then
//                        m_count <- m_count - 1
//                    else
//                        let heapIndex = heapInverse.[realIndex]
//                        // swap front to back for removal
//                        swapInplace heapInverse.[realIndex] (m_count)
//                        m_count <- m_count - 1
//                        // re-sort heap, move downward, because the element was taken from bottom anyway
//                        sortHeapDownward heapIndex

//            /// Removes the element with given index from the queue 
//            member this.TryRemove realIndex =
//                //if (m_count > 0) then failwith "IndexedPriorityQueue.Pop: Queue is empty"
//                if ((heapInverse.[realIndex]) <= m_count) then 

//                    if heapInverse.[realIndex]=(m_count) then
//                        m_count <- m_count - 1
//                    else
//                        let heapIndex = heapInverse.[realIndex]
//                        // swap front to back for removal
//                        swapInplace heapInverse.[realIndex] (m_count)
//                        m_count <- m_count - 1
//                        // re-sort heap
//                        sortHeapDownward heapIndex
    
//            /// Put indexOut element outside of the heap and move indexIn element inside the heap
//            member this.Swap indexOut indexIn =
//                if ((heapInverse.[indexOut]) > m_count) then 
//                    failwith "IndexedPriorityQueue.Return: The element was already removed from heap"
//                elif ((heapInverse.[indexIn]) <= m_count) then 
//                    failwith "IndexedPriorityQueue.Return: The element was already inside heap"  
//                else   
//                    swapInplace (heapInverse.[indexOut]) (heapInverse.[indexIn])
//                    if ( objects.[indexIn] > objects.[indexOut] ) then // change here if change MAX MIN sorting
//                        sortDownward indexIn
//                    else 
//                        sortUpward indexIn

//            /// Removes the element with given index from the queue 
//            member this.TryRemoveGroup (indeces: int []) =
//                //let heapIDs = 
//                //    [|for id in indeces ->
//                //        let c = heapInverse.[id]
//                //        if (c > m_count) then 
//                //            failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                //        else
//                //            // swap front to back for removal
//                //            swapInplace c (m_count)
//                //            m_count <- m_count - 1
//                //            c  |] 
//                //    |> Array.sort
//                let heapIDs =
//                    indeces
//                    |> Array.map (fun id -> heapInverse.[id])
//                    |> Array.filter (fun c -> c <= m_count)
//                    |> Array.map (fun c -> 
//                        swapInplace c (m_count)
//                        m_count <- m_count - 1
//                        c)
//                    |> Array.sort
//                // re-sort heap
//                for id in heapIDs do
//                    sortDownward id

//            member this.ReturnGroup (indeces: int[]) =
//                let heapIDs = 
//                    [|for id in indeces ->
//                        let c = heapInverse.[id]
//                        if (c <= m_count) then 
//                            failwith "IndexedPriorityQueue.ReturnGroup: The element was already inside"
//                        else
//                            // swap front to back for removal
//                            swapInplace c (m_count+1)
//                            m_count <- m_count + 1
//                            c  |] 
//                    |> Array.sort
//                // re-sort heap
//                for id in heapIDs do
//                    sortDownward id

//            member this.TryReturnGroup (indeces: int[]) =
//                let heapIDs =
//                    indeces
//                    |> Array.map (fun id -> heapInverse.[id])
//                    |> Array.filter (fun c -> c > m_count)
//                    |> Array.map (fun c -> 
//                        swapInplace c (m_count+1)
//                        m_count <- m_count + 1
//                        c)
//                    |> Array.sort
//                // re-sort heap
//                for id in heapIDs do
//                    sortDownward id

//            /// Removes all elements except those with given index from the queue 
//            member this.LeaveGroup (indeces: int []) =
//                let x = indeces.Length
//                indeces
//                |> Array.map (fun i -> heapInverse.[i])
//                |> Array.sort
//                |> Array.iteri (fun i c ->
//                    //if (c > m_count) then 
//                    //    failwith "IndexedPriorityQueue.Remove: The element was already removed"
//                    //else
//                        // swap front to back for removal
//                        swapInplace c (i+1) )
//                // re-sort heap
//                m_count <- x
//                for id=2 to x do
//                    sortHeapUpward id

//            /// Gets the top element of the queue
//            member this.Top() = 
//                // top of heap [first element is 1, not 0]
//                objects.[heap.[1]]

//            /// Gets the index of the top element of the queue
//            member this.TopIndex() = 
//                // top of heap [first element is 1, not 0]
//                heap.[1]

//            /// Inserts a new value with the given index in the queue
//            member this.Insert index (value:'T) =
//                //if (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Insert: Index %i out of range" index
//                m_count <- m_count + 1
        
//                // add object
//                objects.[index] <- value

//                // add to heap
//                heapInverse.[index] <- m_count
//                heap.[m_count] <- index

//                // update heap
//                sortHeapUpward m_count // the new item is by default in the bottom of the tree, the biggest. So check, if it is not the case, then move it up
        
//            member this.Length = 
//                m_count

//            /// Returns an item of the set with given index
//            member this.Item 
//                with get index = 
//                    if not (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                    objects.[index]
//                and  set index value = 
//                    if not (index < objects.Length && index >= 0) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                    this.Set index  value 
    
//            /// Returns an item of the heap with given index
//            member this.HeapItem index =
//                if (index <= 0) || (index > m_count) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                objects.[heap.[index]]

//            /// Returns an index in original objects order of the heap element with given index
//            member this.HeapItemIndex index =
//                if (index <= 0) || (index > m_count) then failwithf "IndexedPriorityQueue.Item: Index %i out of range" index
//                heap.[index]

//            /// Set internal properties of the type directly    
//            member internal this.SetData (_objects: 'T []) (_heap:int[]) (_heapInverse:int[]) (_m_count: int) = 
//                for i=0 to (n-1) do
//                    objects.[i]       <- _objects.[i] 
//                    heap.[i]          <- _heap.[i]
//                    heapInverse.[i]   <- _heapInverse.[i]
//                heap.[objects.Length] <- _heap.[objects.Length]
//                m_count <- _m_count
    
//            /// deep Copy the variable into a new one
//            member this.DeepCopy() = 
//                let temp = new MinIndexPriorityQueue<'T>(n)
//                temp.SetData objects heap heapInverse m_count
//                temp

//    module General = 

//        open Types
        
//        ////// Prepare data

//        /// transform raw data in proteins experimental data using given transformation function
//        let transformKineticData (transformF: float [] -> float []) (item: Item) =
//            {item with dataL = 
//                        item.dataL 
//                        |> transformF }

//        /// Around-mean transformation
//        let zScoreTransform (data: float []) = 
//            let stats = data |> Seq.ofArray //|> Seq.stats  
//            let mean = stats |> Seq.mean //SummaryStats.mean stats
//            let sd = stats |> Seq.stDev  //SummaryStats.stDevPopulation stats
//            data
//            |> Seq.map (fun x -> (x-mean)/sd)
//            |> Seq.toArray

//        /// get id list from protein list
//        let groupIDFn (items : Item []) = 
//            items |> Array.map (fun ii -> ii.ID) 

//        ////// Calculation

//        /// calculate euclidian distance between two vectors with weight (optional)
//        let weightedEuclidean (weightL: seq<float> option) v1 v2 = 
//                let n = 
//                    v1
//                    |> Seq.length
//                let weightL' =
//                    match weightL with
//                    |Some x -> x
//                    |None -> Seq.initRepeatValue n 1.
//                Seq.zip3 weightL' v1 v2
//                |> Seq.fold (fun d (w12,e1,e2) -> d + w12*((e1 - e2) * (e1 - e2))) 0.
//                |> sqrt


//        /// apply weighted Euclidean distance to create a matrix of two vectors of data
//        let distanceMatrixWeighted weightL (data:float[][]) =
//            let m = Array2D.zeroCreate (data.Length) (data.Length)
//            for rowI in 0..data.Length-1 do
//                for colI in 0..rowI do
//                    let tmp = weightedEuclidean weightL data.[rowI] data.[colI] 
//                    m.[colI,rowI] <- tmp
//                    m.[rowI,colI] <- tmp
//            m

//        /// calculate matrix for a list of Item
//        let distMatrixWeightedOf f weightL (itemOutList: array<Types.Item>) =
//            let listData = 
//                itemOutList
//                |> Array.map (fun i -> i.dataL) 
//            if (listData.Length > 1) then 
//                    f weightL listData 
//            else 
//                    Array2D.zeroCreate 2 2

//        // change here if want smth not MAX
//        /// find the max of dissimilarities for a protein (idCurrent) in a group of Item (idGroup) by their ids


//        // Calculation of gain, given protein kinetics and kinetics of all Item in current and predeccesor node 

//        /// search through matrix and find max dissimilarity for each element in itemsToSum and sum them
//        let dSumFn itemsToSum itemsWhereToFind matrix =
        
//            let findMaxDist idCurrent (idGroup: int array) (matrix: float [,]) = 
//                idGroup
//                |> Array.fold (fun maxSoFar i -> max maxSoFar matrix.[i,idCurrent]) 0.
        
//            itemsToSum
//            |> Array.fold (fun acc i -> acc + (findMaxDist i itemsWhereToFind matrix)) 0.

//        /// calculate step gain between parent and child nodes given the root number with given function for G_s
//        let getStepGainFn fn itemsChild itemsParent (nRoot: int) matrix : float =

//            let dSumF itemsToSum itemsWhereToFind matrix =
        
//                let findMaxDistInMatrix idCurrent (idGroup: int array) (matrix: float [,]) = 
//                    idGroup
//                    |> Array.fold (fun maxSoFar i -> max maxSoFar matrix.[i,idCurrent]) 0.
        
//                itemsToSum
//                |> Array.fold (fun acc i -> acc + (findMaxDistInMatrix i itemsWhereToFind matrix)) 0.

//            let dPredSum = dSumF itemsChild itemsParent matrix
//            let dCurrSum = dSumF itemsChild itemsChild matrix
//            fn dCurrSum dPredSum itemsChild.Length nRoot 

//        /// function to calculate configuration gain
//        let confGainFn (items: Map<string,Types.Node<string,Types.Item>>) =                              /// complex input
//            items
//            |> Map.fold (fun state key value -> state+(value.MaxGain)) 0. 

//        ////// Tree structure

//        /// break the group to find all predefined subbins in the current node, 
//        /// looking at bin list and group by item.next_depth and also find Items, that were left in the current node
//        let rec breakGroup (items: Types.Item array) depth =
    
//            // checking for the tunnels
//            if (items |> Array.forall (fun ii -> ii.OriginalBin.Length>depth+1 && ii.OriginalBin.[depth+1]=items.[0].OriginalBin.[depth+1])) then 
//                let newItems = 
//                    items
//                    |> Array.map (fun x -> 
//                        let newSubbinLabel = sprintf "%s-%s" x.OriginalBin.[depth] x.OriginalBin.[depth+1]
//                        let newBin = [|x.OriginalBin.[0 .. depth-1];[|newSubbinLabel|]; x.OriginalBin.[depth+2 ..]|] |> Array.concat
//                        {x with 
//                            OriginalBin=newBin
//                            BinL=newBin})
//                breakGroup newItems depth
//            else
//                items 
//                |> Array.map (fun i -> 
//                    if i.OriginalBin.Length<=(depth+1) then 
//                        {i with 
//                            OriginalBin = Array.append i.OriginalBin [|sprintf "%s%i" "p" i.ID|]; 
//                            BinL = Array.append i.BinL [|sprintf "%s%i" "p" i.ID|]}
//                    else i)
//                |> Array.groupBy (fun i -> i.OriginalBin.[depth+1])
//                |> Map.ofArray

//        let rec breakGroupHermit (hermitShapeMap: Map<string,int>) (items: Types.Item array) depth =
    
//            //printfn "break for depth %i" depth
//            //printfn "%A" (items |> Array.map (fun i -> i.BinL))

//            if (items |> Array.forall (fun ii -> ii.OriginalBin.Length>depth+1 && ii.OriginalBin.[depth+1]=items.[0].OriginalBin.[depth+1])) 
//            then // there is a tunnel -> delete the tunneling subbin
        
//                let newItems = 
//                    items
//                    |> Array.map (fun x -> 
//                        let newSubbinLabel = sprintf "%s-%s" x.OriginalBin.[depth] x.OriginalBin.[depth+1]
//                        let newBin = [|x.OriginalBin.[0 .. depth-1];[|newSubbinLabel|]; x.OriginalBin.[depth+2 ..]|] |> Array.concat
//                        {x with 
//                            OriginalBin=newBin
//                            BinL=newBin})
//                breakGroupHermit hermitShapeMap newItems depth

//            elif (depth>0 && items.[0].BinL.[depth] |> String.contains "h" )
//            then // we are inside Hermit group -> break into singletons

//                items 
//                |> Array.map (fun i -> 
//                    if i.BinL.Length<=(depth+1) then 
//                        {i with 
//                            OriginalBin = Array.append i.OriginalBin [|sprintf "%s%i" "p" i.ID|]; 
//                            BinL = Array.append i.BinL [|sprintf "%s%i" "p" i.ID|]}
//                    else i)
//                |> Array.groupBy (fun i -> i.BinL.[depth+1])
//                |> Map.ofArray

//            else // no tunnels in the structure, not inside Hermit group -> make a break
        
//                let nSingletons = items |> Array.filter (fun i -> i.BinL.Length<=(depth+1)) |> Array.length

//                if nSingletons>30 then // too many singletons, use Hermit classification first to reduce the complexity
//                    items 
//                    |> Array.map (fun i -> 
//                        if i.BinL.Length<=(depth+1) then 
//                            let hermitClass = hermitShapeMap |> Map.find i.ProteinL.[0]
//                            {i with 
//                                //OriginalBin = Array.append i.OriginalBin [|sprintf "%s%i" "h" hermitClass|]; 
//                                BinL = Array.append i.BinL [|sprintf "%s%i" "h" hermitClass|]}
//                        else i)
//                    |> Array.groupBy (fun i -> i.BinL.[depth+1])
//                    |> Map.ofArray
//                else // general break, according to subbins of the depth
//                    items 
//                    |> Array.map (fun i -> 
//                        if i.OriginalBin.Length<=(depth+1) then 
//                            {i with 
//                                OriginalBin = Array.append i.OriginalBin [|sprintf "%s%i" "p" i.ID|]; 
//                                BinL = Array.append i.BinL [|sprintf "%s%i" "p" i.ID|]}
//                        else i)
//                    |> Array.groupBy (fun i -> i.OriginalBin.[depth+1])
//                    |> Map.ofArray

//        /// Intermediate partition function: integer partition of n to k parts
//        let schemeGenerator n k =
//            let schemeArray = Array.zeroCreate k 
//            let rec loop nRest kRest prevSum maxValue =
//                seq [
//                    if (prevSum=n) || (kRest=0) then
//                        let temp = schemeArray.[0 .. (k-1)]             // to be able to write inside array
//                        yield temp
//                    else
//                        let half = int (ceil((float nRest)/(float kRest)))
//                        let list = List.init ( (min maxValue (nRest-kRest+1)) - half + 1 ) (fun i -> half + i )
//                        for a in list do 
//                            schemeArray.[(k-kRest)] <- a
//                            yield! loop (nRest-a) (kRest-1) (prevSum+a) a 
//                ]
//            loop n k 0 n

//        ////// Create tree

//        // Pure combinatoric task

//        ///Set partitioning
//        let rec partitions l =
//            let rec insertReplace x = function
//                | []             -> []
//                | (y :: ys) as l ->
//                    ((x::y)::ys)::(List.map (fun z -> y::z) (insertReplace x ys))
//            seq {
//                match l with   
//                | []   ->
//                    yield []
//                | h::tail ->
//                    for part in (partitions tail) do
//                        yield [h]::part
//                        yield! insertReplace h part
//            }

//        /// apply lazy partition to a broken groups
//        let partGroup depth (groups: Map<string,Types.Item []>) : Map<string,Types.Item []> seq =
    
//            let rename (list: (string*(Types.Item [])) list) =
//                if list.Length>1 then
//                    let newKey =
//                        list 
//                        |> List.fold (fun state (k,v) -> (sprintf "%s|%s" state k)) (sprintf "mix")  
//                    let newListValue = 
//                        list 
//                        |> List.map (fun (k,v) -> v) 
//                        |> Array.concat      
//                        |> Array.map (fun protein -> {protein with BinL= Array.append protein.BinL.[0 .. depth] [|newKey|]})   
//                    (newKey, newListValue)
//                else
//                    list.[0]
//            groups
//            |> Map.toList
//            |> partitions
//            |> Seq.map (List.map (fun variation -> rename variation) >> Map.ofList )

//    module Clustering =
    
//        open Types

//        /// call hierarchical clustering for singletons
//        let clusterHier (k: int) weight (children: Types.Item list) =
//            let clusters nClusters =
//                children
//                |> List.map (fun protein -> protein.dataL)
//                |> ML.Unsupervised.HierarchicalClustering.generate (General.weightedEuclidean weight) (ML.Unsupervised.HierarchicalClustering.Linker.centroidLwLinker)
//                |> ML.Unsupervised.HierarchicalClustering.cutHClust nClusters
//                |> List.map (List.map (fun i -> children.[ML.Unsupervised.HierarchicalClustering.getClusterId i]))
//            clusters k
//            |> List.map (fun list ->
//                let binName = list |> List.fold (fun acc x -> sprintf "%s|p%i" acc x.ID) "hc" 
//                (binName,list |> Array.ofList))
//            |> Map.ofList
            
//        let onlyClustering kmeanKKZ depth matrixSingletons (singles: Map<string,Node<string,Item>>) gainFn (data: Map<string, Item []>) =
        
//            printfn "new node size = %i" data.Count

//            let parents = singles |> Map.toArray |> Array.map (fun (_, n) -> n.Members) |> Array.concat

//            let rename (list: (string * Item []) []) =
//                if list.Length>1 then
//                    let newKey =
//                        list 
//                        |> Array.fold (fun state (k,v) -> (sprintf "%s|%s" state k)) (sprintf "mix")  
//                    let newListValue = 
//                        list 
//                        |> Array.map (fun (k,v) -> v) 
//                        |> Array.concat      
//                        |> Array.map (fun protein -> {protein with BinL= Array.append protein.BinL.[0 .. depth] [|newKey|]})   
//                    (newKey, newListValue)
//                else
//                    list.[0]    

//            let eval fn matrixSingles (initConf: (string * Item [] ) [] [])  =
//                (initConf
//                |> Array.sumBy (fun cluster ->
//                    if cluster.Length=1 then 
//                        (Map.find (fst cluster.[0]) singles).MaxGain
//                    else
//                        let itemsChild = cluster |> Array.map (snd) |> Array.concat |> General.groupIDFn
//                        let itemsParent = parents |> General.groupIDFn 
//                        General.getStepGainFn fn itemsChild itemsParent itemsParent.Length matrixSingles
//                    ), initConf)

//            let singlesG =
//                singles |> Map.toArray |> Array.sumBy (fun (_,x) -> x.MaxGain)
//            let singlesA =
//                singles |> Map.toArray |> Array.map (fun (s, n) -> [|s,n.Members|])

//            (singlesG, singlesA) :: 
//                ([2 .. (data.Count-1)] 
//                |> List.map (fun i -> 

//                    //printfn "clustering with k = %i" i

//                    data
//                    |> kmeanKKZ i
//                    |> eval gainFn matrixSingletons)
//                )
//            |> Seq.ofList         
//            |> Seq.maxBy (fst)
//            |> snd
//            |> Array.map (fun groupIDs ->     
//                groupIDs 
//                |> rename )
//            |> Map.ofArray

//        let pairwiseCorrAverage (x:matrix) (y:matrix) =
//            let xN = x.Dimensions |> fst
//            let yN = y.Dimensions |> fst
//            let m = Array2D.create xN yN 0.
//            for rowI in 0..(xN-1) do
//                for colI in 0..(yN-1) do
//                    let tmp = General.weightedEuclidean None (x.Row rowI) (y.Row colI) 
//                    m.[rowI,colI] <- tmp
//            m 
//            |> Array2D.array2D_to_seq 
//            |> Seq.average

//        let distMatrix (matrixA: string * matrix) (matrixB: string * matrix) =
//            let mA = snd matrixA
//            let mB = snd matrixB
//            pairwiseCorrAverage mA mB

//        let intiCgroups (input: (string*matrix) array) k : ((string*matrix) []) =

//            let dmatrix = input |> Array.map (fun (bin,data) -> (data |> Matrix.enumerateColumnWise (fun x -> x |> Seq.median) )) |> MatrixTopLevelOperators.matrix
        
//            //let cvmax = // find a feature with the biggest variance and return the (row Number, values of the feature), sorted by the values 
//            //    dmatrix
//            //    |> Matrix.Generic.enumerateColumnWise Seq.var
//            //    |> Seq.zip (Matrix.Generic.enumerateColumnWise id dmatrix)
//            //    |> Seq.maxBy snd
//            //    |> fst
//            //    |> Seq.mapi (fun rowI value -> (rowI,value)) 
//            //    |> Seq.toArray 
//            //    |> Array.sortBy snd

//            let cvmax = // set the most important feature at 24H (with index 1) 
//                dmatrix
//                |> Matrix.Generic.enumerateColumnWise Seq.var
//                |> Seq.zip (Matrix.Generic.enumerateColumnWise id dmatrix)
//                |> Seq.item 1
//                |> fst
//                |> Seq.mapi (fun rowI value -> (rowI,value)) 
//                |> Seq.toArray 
//                |> Array.sortBy snd

//            if cvmax.Length < k then failwithf "Number of data points must be at least %i" k        
//            let chunkSize = cvmax.Length / k
//            let midChunk  = chunkSize / 2
//            [ for i=1 to k do
//                let index = 
//                    match (chunkSize * i) with
//                    | x when x < cvmax.Length -> x - midChunk
//                    | x                       -> chunkSize * (i - 1) + ((cvmax.Length - chunkSize * (i - 1)) / 2)
//                //printfn "Array.lenght = %i and index = %i" cvmax.Length (index-1)
//                yield cvmax.[index-1] |> fst]
//            |> Seq.map (fun rowI -> input.[rowI])
//            |> Seq.toArray

//        /// KKZ deterministic centroid initialization for kMean clustering
//        let initCgroupsKKZ (data: (string*matrix) array) k =
//            let centroid1 =
//                data 
//                |> Array.maxBy 
//                    (fun x -> 
//                        x 
//                        |> snd
//                        |> Matrix.enumerateColumnWise (Seq.mean) 
//                        |> fun xx ->  sqrt ( xx |> Seq.sumBy (fun i -> i*i))
//                    )
//            let LeaveData d c =
//                d |> Array.removeIndex (Array.FindIndex<string*matrix>(d, fun x -> x=c))

//            let rec loop dataRest kRest centroids =
//                if kRest=1 then   
//                    centroids
//                else    
//                    let newC = 
//                        dataRest 
//                        |> Array.map (fun (s,p) -> 
//                            (s,p), centroids |> List.map (fun (sc,c) -> pairwiseCorrAverage (p)  (c)) |> List.min )
//                        |> Array.maxBy snd 
//                        |> fst
//                    loop (LeaveData dataRest newC) (kRest-1) (newC::centroids)

//            loop (LeaveData data centroid1) k [centroid1]
//            |> List.toArray

//        /// Recompute Centroid as average of given sample (for kmeans)
//        let updateCentroid (current: string * matrix) (sample: (string * matrix) []) = // rewrite it in matrix!
//            let size = sample.Length
//            match size with
//            | 0 -> current
//            | _ ->
//                ("", 
//                    sample
//                    |> Array.map (fun (_,x) -> x.ToArray2D() |> Array2D.toJaggedArray)
//                    |> Array.concat
//                    |> fun p -> MatrixTopLevelOperators.matrix p)

//        /// Convert centroids into an initial scheme for K-Mean-Swap
//        let centroidsToScheme (input: Item [] []) (centroid: matrix []) (scheme: int []) : ((string*(Item [])) []) =
//            let sorted = input |> Array.indexed |>  Array.sortByDescending ( fun (i,x) -> Array.length x)
//            let rec loop itemsRest groupID =
//                [|if groupID=scheme.Length then   
//                    let binName = itemsRest |> Array.concat |> Array.map (fun p -> p.ID) |> Array.sort |> fun i -> String.Join("|",i)
//                    yield (binName,itemsRest |> Array.concat)
//                else    
//                    let (itemsCluster,itemsNew) = 
//                        itemsRest 
//                        |> Array.sortByDescending 
//                            (fun i -> pairwiseCorrAverage centroid.[groupID] (i |> Array.map (fun x -> x.dataL) |> MatrixTopLevelOperators.matrix))
//                        |> Array.splitAt scheme.[groupID]
//                    let binName = itemsCluster |> Array.concat |> Array.map (fun p -> p.ID) |> Array.sort |> fun i -> String.Join("|",i)
//                    yield (binName,itemsCluster |> Array.concat)
//                    yield! loop itemsNew (groupID+1)
//                |]
//            loop (input) 0

//        let clustersToCentroidMatrix (clusters: Item [] [] []) : matrix [] =
//            clusters
//            |> Array.map (fun cluster -> 
//                cluster
//                |> Array.map (fun group ->
//                    group |> Array.map (fun x -> x.dataL)
//                    )
//                |> Array.concat
//                |> MatrixTopLevelOperators.matrix
//                )

//        let centroidsToMatrix (centroids: Item [] list) : matrix [] =
//            centroids
//            |> List.toArray
//            |> Array.map (fun centroid ->
//                centroid |> Array.map (fun x -> x.dataL) |> MatrixTopLevelOperators.matrix
//                )

//        let kkzSingle (data: Item []) k =
//            let centroid1 =
//                data |> Array.maxBy (fun x -> sqrt ( x.dataL |> Array.sumBy (fun i -> i*i)))
//            let LeaveData d c =
//                d |> Array.removeIndex (Array.FindIndex<Item>(d, fun x -> x=c))
//            let rec loop dataRest kRest centroids =
//                if kRest=1 then   
//                    centroids
//                else    
//                    let newC = 
//                        dataRest 
//                        |> Array.map  (fun p -> p, centroids |> List.map (fun c -> General.weightedEuclidean None p.dataL c.dataL) |> List.min )
//                        |> Array.maxBy snd 
//                        |> fst
//                    loop (LeaveData dataRest newC) (kRest-1) (newC::centroids)
//            loop (LeaveData data centroid1) k [centroid1]

//        /// give a list of centroids as k the most distant elements of the dataset     
//        let kkz (data: Item [] []) k =
//            let centroid1 =
//                data 
//                |> Array.maxBy 
//                    (fun x -> 
//                        x 
//                        |> Array.map (fun x -> x.dataL) 
//                        |> JaggedArray.transpose 
//                        |> Array.map (Array.average) 
//                        |> fun xx ->  sqrt ( xx |> Array.sumBy (fun i -> i*i))
//                    )
//            let LeaveData d c =
//                d |> Array.removeIndex (Array.FindIndex<Item []>(d, fun x -> x=c))
//            let toMatrix =
//                Array.map (fun i -> i.dataL)
//                >> MatrixTopLevelOperators.matrix

//            let rec loop dataRest kRest centroids =
//                if kRest=1 then   
//                    centroids
//                else    
//                    let newC = 
//                        dataRest 
//                        |> Array.map (fun p -> 
//                            p, centroids |> List.map (fun c -> pairwiseCorrAverage (toMatrix p)  (toMatrix c)) |> List.min )
//                        |> Array.maxBy snd 
//                        |> fst
//                    loop (LeaveData dataRest newC) (kRest-1) (newC::centroids)
//            loop (LeaveData data centroid1) k [centroid1]

//        //let varPart_Single (data: Item []) k =
//        //    let sse (cluster: Item []) =
//        //        let centroid = cluster |> Array.map (fun x -> x.dataL) |> MatrixTopLevelOperators.matrix
//        //        let dist (a: Item) (b: matrix) =
//        //            pairwiseCorrAverage ([a.dataL] |> MatrixTopLevelOperators.matrix) b
//        //        cluster
//        //        |> Array.map 
//        //            (fun i -> (dist i centroid)*(dist i centroid))
//        //        |> Array.average
//        //    let split (cluster: Item []) =
//        //        let featureN = 
//        //            cluster 
//        //            |> Array.map (fun x -> x.dataL) 
//        //            |> MatrixTopLevelOperators.matrix 
//        //            |> Matrix.Generic.enumerateColumnWise Seq.var
//        //            |> Seq.mapi (fun id x -> (id,x))
//        //            |> Seq.maxBy snd
//        //            |> fst
//        //        let featureMean =
//        //            cluster
//        //            |> Array.map (fun x -> x.dataL.[featureN])
//        //            |> Array.average
//        //        cluster
//        //        |> Array.partition (fun x -> x.dataL.[featureN]>featureMean)
//        //    let pq = MaxIndexPriorityQueue<float>(k*2-1) // put a cluster there or SSE of a cluster or SSE*cluster????
//        //    let clusters: Item [] [] = Array.create (k*2-1) [||]
//        //    pq.Insert 0 (sse data)
//        //    clusters.[0] <- data
//        //    [|1 .. (k-1)|] 
//        //    |> Array.iter (fun ik -> 
//        //        let loosest = clusters.[pq.HeapItemIndex 1]
//        //        let newCl = split loosest
//        //        pq.Pop() |> ignore
//        //        pq.Insert (2*ik) (sse (fst newCl))
//        //        pq.Insert (2*ik-1) (sse (snd newCl))
//        //        clusters.[2*ik] <- (fst newCl)
//        //        clusters.[2*ik-1] <- (snd newCl)
//        //        )
//        //    [|1 .. k|]
//        //    |> Array.map (fun x -> clusters.[pq.HeapItemIndex x])


//        //let varPart (data: Item [] []) k =
//        //    let sse (cluster: Item [] []) =
//        //        let centroid = cluster |> Array.map (fun x -> x |> Array.map (fun i -> i.dataL)) |> Array.concat |> MatrixTopLevelOperators.matrix
//        //        let dist (a: Item []) (b: matrix) =
//        //            pairwiseCorrAverage (a |> Array.map (fun i -> i.dataL) |> MatrixTopLevelOperators.matrix) b
//        //        cluster
//        //        |> Array.map 
//        //            (fun i -> (dist i centroid)*(dist i centroid))
//        //        |> Array.average
//        //    let split (cluster: Item [] []) =
//        //        let featureN = 
//        //            cluster 
//        //            |> Array.map (fun x -> x |> Array.map (fun i -> i.dataL) |> JaggedArray.transpose |> Array.map Array.average )
//        //            |> MatrixTopLevelOperators.matrix 
//        //            |> Matrix.Generic.enumerateColumnWise Seq.var
//        //            |> Seq.mapi (fun id x -> (id,x))
//        //            |> Seq.maxBy snd
//        //            |> fst
//        //        let featureMean =
//        //            cluster
//        //            |> Array.map (fun x -> x |> Array.map (fun i -> i.dataL.[featureN]) |> Array.average)
//        //            |> Array.average
//        //        cluster
//        //        |> Array.partition (fun x -> (x |> Array.map (fun i -> i.dataL.[featureN]) |> Array.average)>featureMean)
//        //    let pq = MaxIndexPriorityQueue<float>(k*2-1) // put a cluster there or SSE of a cluster or SSE*cluster????
//        //    let clusters: Item [] [] [] = Array.create (k*2-1) [||]
//        //    pq.Insert 0 (sse data)
//        //    clusters.[0] <- data
//        //    [|1 .. (k-1)|] 
//        //    |> Array.iter (fun ik -> 
//        //        let loosest = clusters.[pq.HeapItemIndex 1]
//        //        let newCl = split loosest
//        //        pq.Pop() |> ignore
//        //        pq.Insert (2*ik) (sse (fst newCl))
//        //        pq.Insert (2*ik-1) (sse (snd newCl))
//        //        clusters.[2*ik] <- (fst newCl)
//        //        clusters.[2*ik-1] <- (snd newCl)
//        //        )
//        //    [|1 .. k|]
//        //    |> Array.map (fun x -> clusters.[pq.HeapItemIndex x])

//        let kmeanGroups (k: int) (children: Map<string,Types.Item []> ) : Map<string,Types.Item []> =

//            let data = children |> Map.toArray |> Array.map (fun (s,ar) -> (s,ar |> Array.map (fun p -> p.dataL) |> MatrixTopLevelOperators.matrix))

//            let clusters = 
//                let c1 = ML.Unsupervised.IterativeClustering.compute distMatrix (intiCgroups) updateCentroid data k
//                let x1 = ML.Unsupervised.IterativeClustering.DispersionOfClusterResult c1
//                [|1 .. 20|]
//                |> Array.fold (fun (disp,best) x -> 
//                    let c = ML.Unsupervised.IterativeClustering.compute distMatrix (intiCgroups) updateCentroid data k
//                    let x = ML.Unsupervised.IterativeClustering.DispersionOfClusterResult c
//                    if x<disp then
//                        (x,c)
//                    else
//                        (disp,best) ) (x1,c1)
//                |> snd

//            data
//            |> Array.map (fun list -> (clusters.Classifier list |> fst),list )
//            |> Array.groupBy (fst)
//            |> Array.map (fun (cID,list) ->
//                let binName = list |> Array.map (fun (cID,(bin,p)) -> bin) |> Array.sort |> fun i -> String.Join("|",i) 
//                let items = list |> Array.map (fun (cID,(bin,p)) -> children |> Map.find bin) |> Array.concat
//                (binName,items))
//            |> Map.ofArray
    
//        let centroidFactory (input: (string*matrix) array) (k: int) : ((string*matrix) []) =
//            let r = new System.Random() 
//            ML.Unsupervised.IterativeClustering.randomCentroids r input k

//        let kmeanGroupsRandom (k: int) (children: Map<string,Types.Item []> ) =

//            let data = children |> Map.toArray |> Array.map (fun (s,ar) -> (s,ar |> Array.map (fun p -> p.dataL) |> MatrixTopLevelOperators.matrix))

//            let clusters = 
//                let c1 = ML.Unsupervised.IterativeClustering.compute distMatrix (centroidFactory) updateCentroid data k
//                let x1 = ML.Unsupervised.IterativeClustering.DispersionOfClusterResult c1
//                [|1 .. 20|]
//                |> Array.fold (fun (disp,best) x -> 
//                    let c = ML.Unsupervised.IterativeClustering.compute distMatrix (centroidFactory) updateCentroid data k
//                    let x = ML.Unsupervised.IterativeClustering.DispersionOfClusterResult c
//                    if x<disp then
//                        (x,c)
//                    else
//                        (disp,best) ) (x1,c1)
//                |> snd

//            data
//            |> Array.map (fun list -> (clusters.Classifier list |> fst),list )
//            |> Array.groupBy (fst)
//            //|> Array.map (fun (cID,list) ->
//            //    let binName = list |> Array.map (fun (cID,(bin,p)) -> bin) |> Array.sort |> fun i -> String.Join("|",i) 
//            //    let items = list |> Array.map (fun (cID,(bin,p)) -> children |> Map.find bin) |> Array.concat
//            //    (binName,items))
//            //|> Map.ofArray
//            |> Array.map (fun (_,list) -> 
//                list 
//                |> Array.map (fun (_,(bin,_)) -> (bin, children |> Map.find bin))
//                )

//        /// Kmean for groups with KKZ centroid init
//        let kmeanGroupsKKZ (k: int) (children: Map<string,Types.Item []> ) =

//            let data = children |> Map.toArray |> Array.map (fun (s,ar) -> (s,ar |> Array.map (fun p -> p.dataL) |> MatrixTopLevelOperators.matrix))

//            let clusters = ML.Unsupervised.IterativeClustering.compute distMatrix (initCgroupsKKZ) updateCentroid data k

//            data
//            |> Array.map (fun list -> (clusters.Classifier list |> fst),list )
//            |> Array.groupBy (fst)
//            |> Array.map (fun (_,list) -> 
//                list 
//                |> Array.map (fun (_,(bin,_)) -> (bin, children |> Map.find bin))
//                )

//        /// 
//        let clusterHierGroups  (children: Map<string,Item []> ) (ks: int list) =
//            //printfn "%i" children.Count
//            let clusters nClusters =
//                let rvToMap (mapA: (string * Item []))  (mapB: (string * Item [])) = 
//                    let mA = mapA |> snd |> Array.map (fun protein -> protein.dataL) |> matrix
//                    let mB = mapB |> snd |> Array.map (fun protein -> protein.dataL) |> matrix
//                    pairwiseCorrAverage mA mB
//                let children' = children |> Map.toArray
//                children'
//                |> ML.Unsupervised.HierarchicalClustering.generate rvToMap (ML.Unsupervised.HierarchicalClustering.Linker.completeLwLinker)
//                |> ML.Unsupervised.HierarchicalClustering.cutHClust nClusters
//                |> List.map (List.map (fun i -> children'.[ML.Unsupervised.HierarchicalClustering.getClusterId i]) >> List.toArray)
//                |> List.toArray
//            ks
//            |> List.map (fun k -> clusters k)

//            //k
//            //|> List.map (fun list ->
//            //    let binName = list |> List.fold (fun acc (bin,proteins) -> sprintf "%s|%s" acc bin) "" 
//            //    let items = list |> List.map snd |> List.toArray |> Array.concat
//            //    (binName,items))
//            //|> Map.ofList

//        let onlyHierClustering depth matrixSingletons (singles: Map<string,Node<string,Item>>) gainFn (data: Map<string, Item []>) =
        
//            printfn "new node size = %i" data.Count

//            let parents = singles |> Map.toArray |> Array.map (fun (_, n) -> n.Members) |> Array.concat

//            let rename (list: (string * Item []) []) =
//                if list.Length>1 then
//                    let newKey =
//                        list 
//                        |> Array.fold (fun state (k,v) -> (sprintf "%s|%s" state k)) (sprintf "mix")  
//                    let newListValue = 
//                        list 
//                        |> Array.map (fun (k,v) -> v) 
//                        |> Array.concat      
//                        |> Array.map (fun protein -> {protein with BinL= Array.append protein.BinL.[0 .. depth] [|newKey|]})   
//                    (newKey, newListValue)
//                else
//                    list.[0]    

//            let eval fn matrixSingles (initConf: (string * Item [] ) [] [])  =
//                (initConf
//                |> Array.sumBy (fun cluster ->
//                    if cluster.Length=1 then 
//                        (Map.find (fst cluster.[0]) singles).MaxGain
//                    else
//                        let itemsChild = cluster |> Array.map (snd) |> Array.concat |> General.groupIDFn
//                        let itemsParent = parents |> General.groupIDFn 
//                        General.getStepGainFn fn itemsChild itemsParent itemsParent.Length matrixSingles
//                    ), initConf)

//            let singlesG =
//                singles |> Map.toArray |> Array.sumBy (fun (_,x) -> x.MaxGain)
//            let singlesA =
//                singles |> Map.toArray |> Array.map (fun (s, n) -> [|s,n.Members|])

//            (singlesG, singlesA) :: 
//                ([2 .. (data.Count-1)] 
//                |>  clusterHierGroups data
//                |> List.map (fun i -> 

//                    //printfn "clustering with k = %i" i.Length

//                    i
//                    |> eval gainFn matrixSingletons)
//                )
//            |> Seq.ofList         
//            |> Seq.maxBy (fst)
//            |> snd
//            |> Array.map (fun groupIDs ->     
//                groupIDs 
//                |> rename )
//            |> Map.ofArray

//    module Walk =

//        open Types
//        open PQ

//        let reflectStateID (matrixA) =
//            matrixA 
//            |> List.distinct
//            |> List.map (fun i -> 
//                i 
//                |> List.indexed 
//                |> List.filter (fun (_,x) -> x=1 ) 
//                |> List.map (fun (i,_) -> i)
//                |> List.toArray
//                )
//            |> List.toArray
    
//        let gainFnn (data: Item [] []) (singleGG: float []) matrixSingles fn (x: int [] []) = 
//                x
//                |> Array.sumBy (fun ids ->
//                        if ids.Length=1 then
//                            singleGG.[ids.[0]]
//                        else    
//                            let itemsChild = ids |> Array.map (fun i -> data.[i]) |> Array.concat |> General.groupIDFn
//                            let itemsParent = data |> Array.concat |> General.groupIDFn 
//                            General.getStepGainFn fn itemsChild itemsParent itemsParent.Length matrixSingles
//                )

//        let matrixG_from_matrixA (data: Item [] []) (singleGG: float []) matrixSingles fn matrixA =
//            let clusterMA = matrixA |> List.distinct
//            let pairArray = Array.allPairs [|0 .. (data.Length-1)|] [|0 .. (clusterMA.Length)|] 
//            let m = JaggedArray.zeroCreate data.Length (clusterMA.Length+1)
//            pairArray
//            |> Array.iter (fun (i,J) -> // that can be substituted by for i=0 to data.Length do for J=0 to (clusterMA.Length+1) do
            
//                let matrixAA = matrixA |> List.map (List.toArray) |> List.toArray

//                let A = (matrixAA.[i] |> Array.indexed |> Array.filter (fun (i,x) -> x=1) |> Array.map fst |> Array.toList) // a was in A
            
//                let B = 
//                    if J<clusterMA.Length then
//                        (clusterMA.[J] |> List.indexed |> List.filter (fun (i,x) -> x=1) |> List.map fst) // b was in B
//                    else 
//                        if A.Length=1 then
//                            []
//                        else
//                            [i]

//                if (A=B) || (B.IsEmpty) then 
//                    m.[i].[J] <- (0., [])
            
//                else
//                    for ii in A do   // move 'a' out of 'A', but leave it stay with itself
//                            matrixAA.[i].[ii] <- 0
//                            matrixAA.[ii].[i] <- 0
//                    matrixAA.[i].[i] <- 1

//                    for jj in B do   // move a in B
//                            matrixAA.[i].[jj] <- 1
//                            matrixAA.[jj].[i] <- 1
//                    let newMA = matrixAA |> Array.map (Array.toList) |> Array.toList
                        
//                    let stateIDs = reflectStateID newMA
//                    let gain =
//                        stateIDs |> gainFnn data singleGG matrixSingles fn

//                    m.[i].[J] <- (gain, newMA)
//                )
//            m

//        let walkingFn dN sN kmeanKKZ depth matrixSingletons (singles: Map<string,Node<string,Item>>) gainFn (data: Map<string, Item []>) = 
    
//            let mutable qDictionary: Map<(int list list),((float*(int list list)) [] [])> = Map.empty

//            let dataGroupsA = data |> Map.toArray

//            let singleGG =
//                dataGroupsA
//                |> Array.map (fun (bin,_) -> 
//                                    let node = singles |> Map.find bin 
//                                    node.MaxGain)

//            /// input data: groups of items
//            let dataGroups = dataGroupsA |> Array.map snd

//            let rename (list: (string*(Types.Item [])) []) =
//                    if list.Length>1 then
//                        let newKey =
//                            list 
//                            |> Array.fold (fun state (k,v) -> (sprintf "%s|%s" state k)) (sprintf "mix")  
//                        let newListValue = 
//                            list 
//                            |> Array.map (fun (k,v) -> v) 
//                            |> Array.concat      
//                            |> Array.map (fun protein -> {protein with BinL= Array.append protein.BinL.[0 .. depth] [|newKey|]})   
//                        (newKey, newListValue)
//                    else
//                        list.[0]    

//            let superFunctionTestG (data: Item [] []) (singleGG: float []) fn matrixSingles initConf  =

//                let initialConfig = 
//                    initConf
//                    |> Array.map 
//                        (Array.map (fun (label,_) -> 
//                            dataGroupsA 
//                            |> Array.findIndex (fun (l,_) -> l=label) )
//                        )

//                //let fileLogInitState = sprintf "Initial state: %A"  (initConf |> Array.map (Array.map fst))
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogInitState])

//                /// adjustency matrix (initialize with initial configuration) 
//                let matrixA =
//                    let m = JaggedArray.zeroCreate data.Length data.Length
//                    for i=0 to (data.Length-1) do
//                        let cluster = initialConfig |> Array.find (fun x -> x |> Array.contains i)
//                        for j=0 to (data.Length-1) do
//                            if (cluster |> Array.contains j) then   
//                                m.[i].[j] <- 1
//                            else
//                                m.[i].[j] <- 0
//                    m
//                    |> Array.map (Array.toList)
//                    |> Array.toList

//                //let mutable qDictionary': Map<(int list list),(QDictionary_GValue [] [])> = Map.empty
//                //((qDictionary'.Item matrixA).[0].[0]).MaxGain <- 0. 

//                let initStateIDs = reflectStateID matrixA
//                let initialState = (gainFnn data singleGG matrixSingles fn initStateIDs, initStateIDs)

//                //let fileLogInit = sprintf "0\t0\tnan\tnan\t0.\t%f" (fst initialState)
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogInit])

//                //let fileLog = sprintf "0\t0\t0.\t%f" (fst initialState)
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLog fileLogName), [fileLog])

//                let clusterMA = matrixA |> List.distinct
//                let pairArray' = Array.allPairs [|0 .. (data.Length-1)|] [|0 .. (clusterMA.Length)|] 

//                let matrixG_origin = matrixG_from_matrixA data singleGG matrixSingles fn matrixA
        
//                qDictionary <- (qDictionary.Add (matrixA, matrixG_origin))

//                let pq_origin =
//                    let n = pairArray'.Length
//                    let p = MaxIndexPriorityQueue<float>(n)
//                    for id=0 to n-1 do 
//                        let (i,ii) = pairArray'.[id]
//                        if (fst matrixG_origin.[i].[ii])>0. then p.Insert id (fst matrixG_origin.[i].[ii]) // load all calculated gains
//                    p

//                let rec loop iStep (mA: int list list) (pairArray: (int*int) []) (mG: (float*(int list list)) [] []) (pq: MaxIndexPriorityQueue<float>) (moved: int []) =
        
//                    let gainCurrent = gainFnn data singleGG matrixSingles fn (reflectStateID mA)
        
//                    let mutable countDirections = 0
            
//                    seq [ while 
//                        (pq.Length>0) 
//                        && (pq.Top()>0.) 
//                        && (countDirections<dN) && (iStep<sN) do // (pq.Top() > gainCurrent) && (countDirections<3) && (iStep<6)
                
//                            countDirections <- countDirections + 1
                    
//                            // order represents the moving: a - moved element, b - target cluster
                                     

//                            //let fileLogStep = sprintf "%i\t%i\t%i\t%i\t%f\t%f" iStep countDirections a b gainCurrent (pq.Top())
//                            //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogStep])

//                            let (a,b) = 
//                                while (pq.Length>0) 
//                                    && (qDictionary |> Map.containsKey (snd mG.[fst pairArray.[pq.TopIndex()]].[snd pairArray.[pq.TopIndex()]])) do
//                                        pq.Pop() |> ignore

//                                if pq.Length=0 then 
//                                    (-1,-1)
//                                else
//                                    pairArray.[pq.TopIndex()]
                        
//                            if (a,b)=(-1,-1) then // if the pq is empty and no new states are found
//                                yield! []
//                            else

//                                let mA_new = snd mG.[a].[b]

//                                //let fileStep = sprintf "%i\t%i\t%f\t%f" iStep countDirections gainCurrent (pq.Top())
//                                //File.AppendAllLines((sprintf "%s%s.txt" pathLog fileLogName), [fileStep])

//                                pq.Pop() |> ignore // pq will be used for other directiones in while loop

//                                // find all values in mG with the same state and exclude possibility to go there again 
//                                // by adding all a's in moved (don't change mG!) and removing duplicate states from pq
//                                let all_a = 
//                                    mG 
//                                    |> Array.indexed 
//                                    |> Array.filter (fun (_, vL) -> (vL |> Array.contains mG.[a].[b]) )
//                                    |> Array.map (fun (i,vl) ->
//                                                let jjj = vl |> Array.findIndex (fun v -> v = mG.[a].[b])
//                                                pq.TryRemove ((i*mG.[0].Length)+jjj)
//                                                i )

//                            //if (qDictionary |> Map.containsKey mA_new) then
                        
//                            //    //let fileLogState = sprintf "%A was already visited, no step further" (reflectStateID mA_new)
//                            //    //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogState]) 
                        
//                            //    yield! [] // how to get rid of the unnecessary empty lists? 
//                            //else
                    
//                                //let fileLogState = sprintf "%A" (reflectStateID mA_new)
//                                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogState]) 

//                                let clusterMA_new = mA_new |> List.distinct
//                                let pairArrayNew = Array.allPairs [|0 .. (data.Length-1)|] [|0 .. (clusterMA_new.Length)|] 

//                                let matrixG = matrixG_from_matrixA data singleGG matrixSingles fn mA_new
                        
//                                qDictionary <- (qDictionary.Add (mA_new, matrixG))
                        
//                                let pq_new = 
//                                    let n = pairArrayNew.Length
//                                    let p = MaxIndexPriorityQueue<float>(n)
//                                    for j=0 to n-1 do 
//                                        let (i,ii) = pairArrayNew.[j]
//                                        let gain = fst matrixG.[i].[ii]
//                                        if gain>0. then
//                                            p.Insert j (gain) // load all gains except of redundant
//                                    p
                        
//                                let new_moved = Array.append all_a moved |> Array.distinct

//                                new_moved
//                                |> Array.iter (fun i ->
//                                    let indices = [|(i * matrixG.[0].Length) .. (i * (matrixG.[0].Length) + matrixG.[0].Length - 1)|]
//                                    pq_new.TryRemoveGroup indices
//                                )

//                                let configuration = reflectStateID mA_new
//                                let gain = gainFnn data singleGG matrixSingles fn (configuration)
//                                let stats = (gain, configuration) 
                            
//                                yield (stats)
//                                yield! loop (iStep+1) mA_new pairArrayNew matrixG pq_new new_moved
//                    ]
    
//                Seq.appendSingleton (loop 1 matrixA pairArray' matrixG_origin pq_origin [||]) initialState 

//            (seq [singleGG |> Array.sum, Array.init data.Count (fun i -> [|i|])]) :: 
//                ([2 .. (data.Count-1)] 
//                |> List.map (fun i -> 

//                    //let fileLogKMEAN = sprintf "Kmean with k=%i"  i
//                    //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogKMEAN])

//                    data
//                    |> kmeanKKZ i
//                    |> superFunctionTestG dataGroups singleGG gainFn matrixSingletons)
//                )
//            |> Seq.ofList
//            |> Seq.concat           
//            |> Seq.maxBy (fst)
//            |> snd
//            |> Array.map (fun groupIDs ->     
//                groupIDs 
//                |> Array.map (fun groupID -> 
//                    dataGroupsA.[groupID])
//                |> rename )
//            |> Map.ofArray

    
//        let walkingHC_Fn hcFn depth matrixSingletons (singles: Map<string,Node<string,Item>>) gainFn (dataM: Map<string, Item []>) = 
    
//            let mutable qDictionary: Map<(int list list),((float*(int list list)) [] [])> = Map.empty

//            let delta = 0.06
//            let nBest = 1 // 3
//            let nDir = 1
//            let nSteps = dataM.Count

//            let dataGroupsA = dataM |> Map.toArray

//            let singleGG =
//                dataGroupsA
//                |> Array.map (fun (bin,_) -> 
//                                    let node = singles |> Map.find bin 
//                                    node.MaxGain)

//            /// input data: groups of items
//            let dataGroups = dataGroupsA |> Array.map snd

//            let rename (list: (string*(Types.Item [])) []) =
//                    if list.Length>1 then
//                        let newKey =
//                            list 
//                            |> Array.fold (fun state (k,v) -> (sprintf "%s|%s" state k)) (sprintf "mix")  
//                        let newListValue = 
//                            list 
//                            |> Array.map (fun (k,v) -> v) 
//                            |> Array.concat      
//                            |> Array.map (fun protein -> {protein with BinL= Array.append protein.BinL.[0 .. depth] [|newKey|]})   
//                        (newKey, newListValue)
//                    else
//                        list.[0]    

//            let superFunctionTestG (data: Item [] []) (singleGG: float []) fn matrixSingles initConf  =

//                let initialConfig = 
//                    initConf
//                    |> Array.map 
//                        (Array.map (fun (label,_) -> 
//                            dataGroupsA 
//                            |> Array.findIndex (fun (l,_) -> l=label) )
//                        )

//                //let fileLogInitState = sprintf "Initial state: %A"  (initConf |> Array.map (Array.map fst))
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogInitState])

//                /// adjustency matrix (initialize with initial configuration) 
//                let matrixA =
//                    let m = JaggedArray.zeroCreate data.Length data.Length
//                    for i=0 to (data.Length-1) do
//                        let cluster = initialConfig |> Array.find (fun x -> x |> Array.contains i)
//                        for j=0 to (data.Length-1) do
//                            if (cluster |> Array.contains j) then   
//                                m.[i].[j] <- 1
//                            else
//                                m.[i].[j] <- 0
//                    m
//                    |> Array.map (Array.toList)
//                    |> Array.toList

//                //let mutable qDictionary': Map<(int list list),(QDictionary_GValue [] [])> = Map.empty
//                //((qDictionary'.Item matrixA).[0].[0]).MaxGain <- 0. 

//                let initStateIDs = reflectStateID matrixA
//                let initialState = (gainFnn data singleGG matrixSingles fn initStateIDs, initStateIDs)

//                //let fileLogInit = sprintf "0\t0\tnan\tnan\t0.\t%f" (fst initialState)
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogInit])

//                //let fileLog = sprintf "0\t0\t0.\t%f" (fst initialState)
//                //File.AppendAllLines((sprintf "%s%s.txt" pathLog fileLogName), [fileLog])

//                let clusterMA = matrixA |> List.distinct
//                let pairArray' = Array.allPairs [|0 .. (data.Length-1)|] [|0 .. (clusterMA.Length)|] 

//                let matrixG_origin = matrixG_from_matrixA data singleGG matrixSingles fn matrixA
        
//                qDictionary <- (qDictionary.Add (matrixA, matrixG_origin))

//                let pq_origin =
//                    let n = pairArray'.Length
//                    let p = MaxIndexPriorityQueue<float>(n)
//                    for id=0 to n-1 do 
//                        let (i,ii) = pairArray'.[id]
//                        if (fst matrixG_origin.[i].[ii])>0. then p.Insert id (fst matrixG_origin.[i].[ii]) // load all calculated gains
//                    p

//                let rec loop iStep (mA: int list list) (pairArray: (int*int) []) (mG: (float*(int list list)) [] []) (pq: MaxIndexPriorityQueue<float>) (moved: int []) =
        
//                    let gainCurrent = gainFnn data singleGG matrixSingles fn (reflectStateID mA)
        
//                    let mutable countDirections = 0
            
//                    seq [ while 
//                        (pq.Length > 0) 
//                        && (pq.Top() > (gainCurrent - delta*gainCurrent) ) // no sinking lower delta
//                        && (countDirections < nDir) // max direction checked = 1
//                        && (iStep < nSteps ) //&& (iStep<5) // max path length = 5
//                        do 
                
//                            countDirections <- countDirections + 1
                    
//                            // order represents the moving: a - moved element, b - target cluster
                                     

//                            //let fileLogStep = sprintf "%i\t%i\t%i\t%i\t%f\t%f" iStep countDirections a b gainCurrent (pq.Top())
//                            //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogStep])

//                            let (a,b) = 
//                                while (pq.Length>0) 
//                                    && (qDictionary |> Map.containsKey (snd mG.[fst pairArray.[pq.TopIndex()]].[snd pairArray.[pq.TopIndex()]])) do
//                                        pq.Pop() |> ignore

//                                if pq.Length=0 then 
//                                    (-1,-1)
//                                else
//                                    pairArray.[pq.TopIndex()]
                        
//                            if (a,b)=(-1,-1) then // if the pq is empty and no new states are found
//                                yield! []
//                            else

//                                let mA_new = snd mG.[a].[b]

//                                //let fileStep = sprintf "%i\t%i\t%f\t%f" iStep countDirections gainCurrent (pq.Top())
//                                //File.AppendAllLines((sprintf "%s%s.txt" pathLog fileLogName), [fileStep])

//                                pq.Pop() |> ignore // pq will be used for other directiones in while loop

//                                // find all values in mG with the same state and exclude possibility to go there again 
//                                // by adding all a's in moved (don't change mG!) and removing duplicate states from pq
//                                let all_a = 
//                                    mG 
//                                    |> Array.indexed 
//                                    |> Array.filter (fun (_, vL) -> (vL |> Array.contains mG.[a].[b]) )
//                                    |> Array.map (fun (i,vl) ->
//                                                let jjj = vl |> Array.findIndex (fun v -> v = mG.[a].[b])
//                                                pq.TryRemove ((i*mG.[0].Length)+jjj)
//                                                i )

//                            //if (qDictionary |> Map.containsKey mA_new) then
                        
//                            //    //let fileLogState = sprintf "%A was already visited, no step further" (reflectStateID mA_new)
//                            //    //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogState]) 
                        
//                            //    yield! [] // how to get rid of the unnecessary empty lists? 
//                            //else
                    
//                                //let fileLogState = sprintf "%A" (reflectStateID mA_new)
//                                //File.AppendAllLines((sprintf "%s%s.txt" pathLOG fileSubName), [fileLogState]) 

//                                let clusterMA_new = mA_new |> List.distinct
//                                let pairArrayNew = Array.allPairs [|0 .. (data.Length-1)|] [|0 .. (clusterMA_new.Length)|] 

//                                let matrixG = matrixG_from_matrixA data singleGG matrixSingles fn mA_new
                        
//                                qDictionary <- (qDictionary.Add (mA_new, matrixG))
                        
//                                let pq_new = 
//                                    let n = pairArrayNew.Length
//                                    let p = MaxIndexPriorityQueue<float>(n)
//                                    for j=0 to n-1 do 
//                                        let (i,ii) = pairArrayNew.[j]
//                                        let gain = fst matrixG.[i].[ii]
//                                        if gain>0. then
//                                            p.Insert j (gain) // load all gains except of redundant
//                                    p
                        
//                                let new_moved = Array.append all_a moved |> Array.distinct

//                                new_moved
//                                |> Array.iter (fun i ->
//                                    let indices = [|(i * matrixG.[0].Length) .. (i * (matrixG.[0].Length) + matrixG.[0].Length - 1)|]
//                                    pq_new.TryRemoveGroup indices
//                                )

//                                let configuration = reflectStateID mA_new
//                                let gain = gainFnn data singleGG matrixSingles fn (configuration)
//                                let stats = (gain, configuration) 

//                                //stepCount <- stepCount + 1

//                                yield (stats)
//                                yield! loop (iStep+1) mA_new pairArrayNew matrixG pq_new new_moved
//                    ]
    
//                Seq.appendSingleton (loop 1 matrixA pairArray' matrixG_origin pq_origin [||]) initialState 

//            //let paralFn i =
//            //    async {return (i |> superFunctionTestG dataGroups singleGG gainFn matrixSingletons)}

//            //(seq [singleGG |> Array.sum, Array.init dataM.Count (fun i -> [|i|])]) :: 
//            //    ([2 .. 4 .. (dataM.Count-1)] 
//            //    |> hcFn dataM
//            //    |> List.toArray
//            //    |> Array.map (fun i -> paralFn i)
//            //    |> Async.Parallel
//            //    |> Async.RunSynchronously
//            //    |> List.ofArray
//            //    )

//            let eval fn matrixSingles (initConf: (string * Item [] ) [] [])  =
//                (initConf
//                |> Array.sumBy (fun cluster ->
//                    if cluster.Length=1 then 
//                        (Map.find (fst cluster.[0]) singles).MaxGain
//                    else
//                        let itemsChild = cluster |> Array.map (snd) |> Array.concat |> General.groupIDFn
//                        let itemsParent = dataGroups |> Array.concat |> General.groupIDFn 
//                        General.getStepGainFn fn itemsChild itemsParent itemsParent.Length matrixSingles
//                    ), initConf)

//            (seq [singleGG |> Array.sum, Array.init dataM.Count (fun i -> [|i|])]) :: 
//                ([2 .. (dataM.Count-1)] 
//                |> hcFn dataM
//                |> List.sortByDescending ((eval gainFn matrixSingletons) >> fst)
//                |> fun x -> if x.Length>(nBest-1) then x.[0 .. (nBest-1)] else x
//                |> List.map (fun i -> i |> superFunctionTestG dataGroups singleGG gainFn matrixSingletons)
//                )
//            |> Seq.ofList
//            |> Seq.concat           
//            |> Seq.maxBy (fst)
//            |> snd
//            |> Array.map (fun groupIDs ->     
//                groupIDs 
//                |> Array.map (fun groupID -> 
//                    dataGroupsA.[groupID])
//                |> rename )
//            |> Map.ofArray
    
//    module Main =

//        open Types

//        /// create tree function with two modes: MM - just read and show original MapMan ontology;
//        /// SSN - process the MM tree into optimal SSN structure
//        /// gainFn - gain formula, 
//        /// kmeanswapFn - function for swapping,
//        /// clusterFn - function for pre-clustering in case of more than 50 singletons as leaves
//        let createTree gainFn (weight: seq<float> option) (mode: Types.Mode) (rootGroup: Types.Item array) = 
        
//            let nRoot = rootGroup.Length

//            let matrix = 
//                rootGroup
//                |> General.distMatrixWeightedOf General.distanceMatrixWeighted weight

//            // calculation for one node    
//            let rec loop (nodeMembers: Types.Item array) depth dPredSum =
        
//                /// sum of max dist within a node 
//                let dCurrSum = General.dSumFn (General.groupIDFn nodeMembers) (General.groupIDFn nodeMembers) matrix

//                /// to calc step from parent to current node
//                let stepGain = (gainFn dCurrSum dPredSum nodeMembers.Length nRoot)

//                let children = 
//                    match mode with
//                    |MM_orig -> // raw MapMan ontology without leaves breaking
//                        let map = 
//                            nodeMembers 
//                            |> Array.filter (fun i -> i.BinL.Length > depth+1)
//                            |> (fun i -> 
//                                    match i with
//                                    |[||] -> 
//                                        Map.empty
//                                    |_ ->
//                                        i
//                                        |> Array.groupBy (fun i -> i.BinL.[depth+1]) 
//                                        |> Map.ofArray)
//                        if map=Map.empty then
//                            Map.empty
//                        else 
//                            map
//                            |> Map.map (fun key nodes -> 
//                                                let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                                loop nodes (depth+1) dPredSum')
//                    |MM -> // MapMan with broken leaves
//                        if nodeMembers.Length=1 then
//                            Map.empty
//                        else 
//                            (General.breakGroup nodeMembers depth)
//                            |> Map.map (fun key nodes -> 
//                                            let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                            (loop nodes (depth+1) dPredSum'))

//                    |ST_combi -> // without simplification, pure combinatorics
//                        if (nodeMembers.Length=1) 
//                                || (nodeMembers.Length=0) 
//                                || (String.contains "mix" (String.Concat nodeMembers.[0].BinL)) 
//                                || (String.contains "c" (String.Concat nodeMembers.[0].BinL)) 
//                                || (String.contains "|" (String.Concat nodeMembers.[0].BinL)) then 
//                            Map.empty
//                        else 
//                            (General.breakGroup nodeMembers depth)
//                            |> General.partGroup depth
//                            |> Seq.fold (fun (singles,best) i -> 
//                                let newNodes = 
//                                    if singles=Map.empty then
//                                        i
//                                        |> Map.fold (fun state key nodes ->
//                                            let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                            state 
//                                            |> Map.add key (loop nodes (depth+1) dPredSum')) (Map.empty)
//                                    else
//                                        i
//                                        |> Map.fold (fun state key nodes ->
//                                            match (singles.TryFind key) with
//                                            | None ->
//                                                let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                                state 
//                                                |> Map.add key (loop nodes (depth+1) dPredSum')
//                                            | Some x ->
//                                                state 
//                                                |> Map.add key x                                                                   
//                                        ) (Map.empty)
                                                                    
//                                let best' =
//                                    if (General.confGainFn newNodes) > (General.confGainFn best) then  // compare configuration gains to get the best
//                                        newNodes  
//                                    else 
//                                        best
//                                if (singles = Map.empty) then
//                                    (newNodes, best')
//                                else
//                                    (singles, best')
                                            
//                            ) (Map.empty,Map.empty) // here as state should be this singles (first) saved and optimal conf
//                            |> snd

//                    |ST_walk (clusterFn, walkFn) -> // Walk with clusterFn results as starting points
//                        if (nodeMembers.Length=1) 
//                                || (nodeMembers.Length=0) 
//                                || (String.contains "mix" (String.Concat nodeMembers.[0].BinL)) 
//                                || (String.contains "c" (String.Concat nodeMembers.[0].BinL)) 
//                                || (String.contains "|" (String.Concat nodeMembers.[0].BinL)) then 
//                            Map.empty
//                        else 
//                            (General.breakGroup nodeMembers depth)
//                            |> (fun x -> 
                                                
//                                let singles = 
//                                    x
//                                    |> Map.fold (fun state key nodes ->
//                                            let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                            state 
//                                            |> Map.add key (loop nodes (depth+1) dPredSum')) (Map.empty)

//                                (walkFn clusterFn) depth matrix singles gainFn x 
//                                |> Map.fold (fun state key nodes ->
//                                    match (singles.TryFind key) with
//                                    | None ->
//                                        let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                        state 
//                                        |> Map.add key (loop nodes (depth+1) dPredSum')
//                                    | Some x ->
//                                        state 
//                                        |> Map.add key x                                                                   
//                                ) (Map.empty) )

//                    |ST (shape, clusterFn, walkFn) -> // Walk with clusterFn results as starting points, and singletons are preclustered according to the shape

//                        let clearHfromSubbin (node: Node<string,Item>) =
//                            node.Members
//                            |> Array.map (fun x -> 
//                                if x.OriginalBin.Length<=(depth+1) then 
//                                    sprintf "p%i" x.ID
//                                else
//                                    x.OriginalBin.[depth+1])
//                            |> Array.distinct
//                            |> fun ar ->
//                                if ar.Length=1 then 
//                                    sprintf "%s" ar.[0]
//                                else
//                                    ar
//                                    |> Array.fold (fun acc x -> sprintf "%s|%s" acc x) "mix"

//                        let changeLastSubbin newBin (node: Node<string,Item>) =
//                            let lastN = node.Members.[0].BinL.Length - 1
//                            let x = Array.copy node.Members.[0].BinL
//                            x.[lastN] <- newBin
//                            x

//                        let removeBinBeforeLast item =
//                            let beforeLastN = item.BinL.Length - 2
//                            let x = Array.copy item.BinL
//                            x |> Array.removeIndex beforeLastN

//                        if (nodeMembers.Length=1) 
//                                || (nodeMembers.Length=0) 
//                                || (String.contains "|" (String.Concat nodeMembers.[0].BinL)) then 
//                            Map.empty
//                        else 
//                            (General.breakGroupHermit shape nodeMembers depth)
//                            |> (fun x -> 
                                                
//                                let singles = 
//                                    x
//                                    |> Map.fold (fun state key nodes ->
//                                            let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                            state 
//                                            |> Map.add key (loop nodes (depth+1) dPredSum')) (Map.empty)

//                                (walkFn clusterFn) depth matrix singles gainFn x 
//                                |> Map.fold (fun state key nodes ->
//                                    match (singles.TryFind key) with
//                                    | None ->
//                                        let dPredSum' = General.dSumFn (General.groupIDFn nodes) (General.groupIDFn nodeMembers) matrix
//                                        state 
//                                        |> Map.add key (loop nodes (depth+1) dPredSum')
//                                    | Some x ->
//                                        state 
//                                        |> Map.add key x                                                                   
//                                ) (Map.empty) )
//                        |> Map.toArray
//                        |> Array.map (fun (bin,node) -> 
//                            if (bin |> String.contains "h") then
//                                if node.Children=Map.empty then
//                                    //only delete h from last bin name in bin and members
//                                    let newBin = clearHfromSubbin node
//                                    let newBinL = changeLastSubbin newBin node
//                                    [|(newBin,{node with Members=(node.Members |> Array.map (fun item -> {item with BinL=newBinL}))})|]
//                                else
//                                    // move child's children one level higher with renaming members' bins and recalculating step gains
//                                    (node.Children 
//                                    |> Map.toArray 
//                                    |> Array.map (fun (subbin,subnode) -> 
//                                        let newBin = removeBinBeforeLast subnode.Members.[0]
//                                        let newStepGain = General.getStepGainFn gainFn (General.groupIDFn subnode.Members) (General.groupIDFn nodeMembers) nodeMembers.Length matrix
//                                        (subbin, {subnode with 
//                                                    Members=(subnode.Members |> Array.map (fun subitem -> {subitem with BinL=newBin})); 
//                                                    StepGain=newStepGain;
//                                                    MaxGain=newStepGain})))
//                            else
//                                // no changes
//                                [|(bin,node)|] )
//                        |> Array.concat
//                        |> Map.ofArray


//                let confGain = General.confGainFn children
//                let ch = 
//                    match mode with
//                    |MM -> 
//                        children
//                    |MM_orig -> 
//                        children
//                    |_ -> 
//                        if  (confGain > stepGain) then 
//                            children;
//                        else 
//                            Map.empty
//                let mem = 
//                    match mode with
//                    |MM -> 
//                        nodeMembers
//                    |MM_orig -> 
//                        nodeMembers
//                    |_ -> 
//                        if (ch |> Map.isEmpty) then 
//                            nodeMembers 
//                        else 
//                            ch |> Map.toArray |> Array.map (fun (_,x) -> x.Members) |> Array.concat
//                {
//                Members = mem;
//                Children = ch;
//                StepGain = stepGain; 
//                ConfGain = (confGain, children |> Map.toList |> List.map fst);
//                MaxGain = max stepGain confGain;
//                }
    
//            loop rootGroup 0 0.

//        /// calculation function, with IC defined
//        let getStepGainNodeSetnR setNR dCurrSum dPredSum numberCurr numberRoot =
//            let nC = float numberCurr
//            let nR = float setNR
//            let deltaDist = dPredSum - dCurrSum
//            let deltaSpec = -((nC/nR)*log2(nC/nR)+((nR-nC)/nR)*log2((nR-nC)/nR))
//            if numberCurr=numberRoot then
//                0.
//            else
//                deltaDist*deltaSpec

//        /// create tree with non-processed MapMan nodes without calculations 
//        let createTreeMMnoCalc (rootGroup: Types.Item array) = 
        
    
//            // calculation for one node    
//            let rec loop (nodeMembers: Types.Item array) depth =
        
//                let children = 
            
//                        let map = 
//                            nodeMembers 
//                            |> Array.filter (fun i -> i.BinL.Length > depth+1)
//                            |> (fun i -> 
//                                    match i with
//                                    |[||] -> 
//                                        Map.empty
//                                    |_ ->
//                                        i
//                                        |> Array.groupBy (fun i -> i.BinL.[depth+1]) 
//                                        |> Map.ofArray)
//                        if map=Map.empty then
//                            Map.empty
//                        else 
//                            map
//                            |> Map.map (fun key nodes -> 
                                        
//                                                loop nodes (depth+1) )
            


//                {
//                Members = nodeMembers;
//                Children = children;
//                StepGain = 0.; 
//                ConfGain = (0., children |> Map.toList |> List.map fst);
//                MaxGain = 0.;
//                }
    
//            loop rootGroup 0

//        //// call the main function, 
//        //// data is an experimental dataset for one functional group,
//        //// setN is usually the size of the whole dataset (can be agjusted)

//        /// Original (no preprocessed) MapMan structure
//        let readMM_original setN data  = createTree (getStepGainNodeSetnR setN)  (None) Types.Mode.MM_orig data

//        /// Preprocessed MapMan structure
//        let readMM setN data  = createTree (getStepGainNodeSetnR setN)  (None) Types.Mode.MM data

//        /// ST with pure combinatorics
//        let getST_combi setN data = createTree (getStepGainNodeSetnR setN) (None) Types.Mode.ST_combi data

//        /// ST with SSSWalk (no preclustering)
//        let getST_onlyWalk setN data = createTree (getStepGainNodeSetnR setN) None (ST_walk (Clustering.clusterHierGroups, Walk.walkingHC_Fn)) data

//        /// ST with preclustering and SSSWalk
//        let getST setN data shape = createTree (getStepGainNodeSetnR setN) None (ST (shape, Clustering.clusterHierGroups, Walk.walkingHC_Fn)) data

//    module Analysis =
    
//        open Types
//        open Main
//        open General

//        module Tree =

//            /// Generic tree node empty
//            let emptyP = { Members = [||] ; MaxGain = 0.; StepGain = 0.; ConfGain = 0.,[]; Children = Map.empty }

//            ///
//            let mapTreeToList f tree' =
//                let rec loop depth tree =
//                    [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (f tree)
//                        | _  -> 
//                                yield (f tree)
//                                for child in tree.Children do                                        
//                                    yield! loop (depth+1) child.Value 
//                    ]
//                loop 0 tree'

//            /// take a tree and return a list of (depth*(node as a tree)) -> very memory-consuming!!!
//            let mapTreeFlat tree' =
//                let rec loop depth tree =
//                    [|match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (depth, tree)
//                        | _  -> 
//                                yield (depth, tree)
//                                for child in tree.Children do                                        
//                                    yield! loop (depth+1) child.Value 
//                    |]
//                loop 0 tree'

//            /// take a tree and flat it by return a list of (depth*(node with empty children tree)
//            let mapTreeFlat' tree' =
//                let rec loop depth tree =
//                    [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (depth, tree)
//                        | _  -> 
//                                yield (depth, {tree with Children=(Map.ofList ["",emptyP])})
//                                for child in tree.Children do                                        
//                                    yield! loop (depth+1) child.Value 
//                    ]
//                loop 0 tree'

//            /// take a tree and return a list of (depth*(members of node))
//            let mapTreeMembers tree' =
//                let rec loop depth tree =
//                    [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (depth, tree.Members)
//                        | _  -> 
//                                yield (depth, tree.Members)
//                                for child in tree.Children do                                        
//                                    yield! loop (depth+1) child.Value 
//                    ]
//                loop 0 tree'

//            /// map node bins
//            let mapTreeBins tree' =
//                let rec loop depth tree acc =
//                    [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (depth, acc)
//                        | _  -> 
//                                yield (depth, acc)
//                                for child in tree.Children do                                        
//                                    yield! loop (depth+1) child.Value (child.Key::acc)
//                    ]
//                loop 0 tree' []

//            /// flatten the tree with complete subbin label
//            let mapTreeToBinsMembers tree' =
//                let rec loop tree acc =
//                    [|match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield (acc |> List.rev, tree.Members)
//                        | _  -> 
//                                //yield (depth, acc, tree.Member)
//                                for child in tree.Children do                                        
//                                    yield! loop child.Value (child.Key::acc)
//                    |]
//                loop tree' []
//                |> Array.map (fun (bin,xList) -> xList |> Array.map (fun x -> bin,x) )
//                |> Array.concat

//            /// find node as a tree with given keyPath 
//            let findNode (keyPath: 'a list) tree = 
//                let rec loop (keyPath: 'a list) tree = 
//                    match keyPath with
//                    | [] -> tree
//                    | k::key ->             
//                        match Map.tryFind k tree.Children with
//                        | Some tree -> loop key tree
//                        | None -> emptyP
//                loop keyPath.Tail tree

//            /// find nodeMembers with given keyPath 
//            let findNodeMembers (keyPath: 'a list) tree = 
//                let rec loop (keyPath: 'a list) tree = 
//                    match keyPath with
//                    | [] -> tree.Members
//                    | k::key ->             
//                        match Map.tryFind k tree.Children with
//                        | Some tree -> loop key tree
//                        | None -> [||]
//                loop keyPath.Tail tree

//            /// return a list of all nodes' sizes
//            let rec filterSizesList tree =
//                [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield tree.Members.Length
//                        | _  -> 
//                                yield tree.Children.Count
//                                for child in tree.Children do                                        
//                                    yield! filterSizesList child.Value 
//                ]

//            /// count number of children for each node in a tree (0's are not shown)
//            let rec filterChildrenList tree =
//                [match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield 0
//                        | _  -> 
//                                yield tree.Children.Count
//                                for child in tree.Children do                                        
//                                    yield! filterChildrenList child.Value 
//                ]
//                |> List.filter (fun i -> i <> 0)

//            /// filter all leaves as flatted tree
//            let rec filterLeaves tree =
//                [|match tree.Children with
//                        | x when x = Map.empty -> 
//                                yield tree.Members
//                        | _  -> 
//                                for child in tree.Children do                                        
//                                    yield! filterLeaves child.Value 
//                |]

//            /// count pairs, not in the same clusters between trees. It reqiures all input items to be identical in both trees 
//            let treeComparison (treeA: Node<string,Item>) (treeB: Node<string, Item>) : int =
//                let m tree = // adjucency matrix for the tree
//                    let idBin = 
//                        tree 
//                        |> filterLeaves |> Array.concat |> Array.map (fun x -> x.ID,x.BinL) |> Array.sortBy fst
//                    idBin |> Array.allPairs idBin |> Array.map (fun ((id1,bin1),(id2,bin2)) -> if bin1=bin2 then 1 else 0)
//                let lA = m treeA 
//                let lB = m treeB 
//                Array.zip lA lB |> Array.sumBy (fun ((a),(b)) -> if a=b then 0 else 1)

//        module DxC =

//            let findMaxDistInMatrix idCurrent (idGroup: int array) (matrix: float [,]) = 
//                    idGroup
//                    |> Array.fold (fun maxSoFar i -> max maxSoFar matrix.[i,idCurrent]) 0.

//            let pointDxC (matrix: float [,]) (nodes: Types.Item [] []) =
//                let nTotal = sqrt (float (matrix.Length))
//                let nCluster = float nodes.Length
//                let complexity = 
//                    nCluster/nTotal
//                let dissimilarityFn node =
//                    node
//                    |> Array.map (fun i -> findMaxDistInMatrix i.ID (groupIDFn node) matrix)
//                    |> Array.max
//                let dissimilarity =
//                    nodes
//                    |> Array.filter (fun i -> i.Length>1)
//                    |> fun x -> 
//                        if x=[||] then 0. 
//                        else x |> Array.averageBy dissimilarityFn
//                (dissimilarity,complexity)

//            let leavesAtDepth (depth: int) (flatTree: (int*Node<string, Item>) []) =
//                flatTree
//                |> Array.choose (fun (i,node) -> 
//                    if i=depth then
//                        Some (node.Members)
//                    elif (i<depth && node.Children=Map.empty) then
//                        Some (node.Members)
//                    else None )

//            let dxc_points_fromMM_Fn matrix tree =
//                let flatTree = 
//                    tree
//                    |> Tree.mapTreeFlat
//                let maxDepth =
//                    flatTree
//                    |> Array.maxBy (fun (i,node) -> i)
//                    |> fst
//                [for a in [0 .. maxDepth] do 
//                    yield 
//                        flatTree
//                        |> leavesAtDepth a 
//                        |> pointDxC matrix]

//            let drawComparePlot pointST (pointsMM: (float*float) list) =
    
//                let stP = 
//                    Chart.Point ([pointST], Name = "ST", Labels = ["MM"])
    
//                let mmP =
//                    let anno = [0 .. (pointsMM.Length-1)] |> List.map string
//                    Chart.Line (pointsMM, Name = "MM", Labels = anno)
   
//                [mmP;stP]
//                |> Chart.Combine 

//            let landscapeData (preData : (string*Node<string,Item>*Node<string,Item>) []) =
//                preData
//                |> Array.map (fun (bin,mmoTree,sstTree) ->
//                    let items = mmoTree.Members |> Array.sortBy (fun x -> x.ID)

//                    let matrixOfN = 
//                        items
//                        |> distMatrixWeightedOf distanceMatrixWeighted (None)
//                        /// normalised
//                    let dissMax = 
//                        matrixOfN
//                        |> Array2D.array2D_to_seq
//                        |> Seq.max

//                    let normMatrix = 
//                        matrixOfN
//                        |> Array2D.map (fun i -> i/dissMax)

//                    let sstPoint = 
//                        sstTree
//                        |> Tree.filterLeaves
//                        |> pointDxC normMatrix
    
//                    let mmoPoints =
//                        dxc_points_fromMM_Fn normMatrix mmoTree

//                    (bin,sstPoint::mmoPoints)
//                    )
//                |> Array.toList

    
//            let drawComparePlot3D (data: (string*(float*float) list) list) =
    
//                let data3d (s,data') =
//                    data'
//                    |> List.map (fun (x,y) -> (s,x,y))

//                let onePath s (data': (string*float*float) list) =
//                    let anno = 
//                        [for a in [0 .. (data'.Tail.Length-1)] do yield string a]
//                    [
//                    Chart.Scatter3d 
//                        ([data'.Head], StyleParam.Mode.Markers_Text, Name = (sprintf "DO_of_%s" s), 
//                        Labels = [sprintf "DO_%s" s], Color = "rgba(50,140,140,1)", TextPosition = StyleParam.TextPosition.BottomCenter);
//                    Chart.Scatter3d (data'.Tail,StyleParam.Mode.Lines_Markers_Text, Name = s, Labels = anno, Color = "rgba(190,140,40,1)")]

//                data
//                |> List.map (fun (s,xy) -> onePath s (data3d (s,xy)))
//                |> List.concat
//                |> Chart.Combine 
//                |> Chart.withX_AxisStyle("Path N")
//                |> Chart.withY_AxisStyle("Dissimilarity")
//                |> Chart.withZ_AxisStyle("Complexity")
//                |> Chart.withSize(1000.,1000.)
//                |> Chart.Show

//            let drawSurface (data: (string*(float*float) list) list) =
//                let sortedData =
//                    data
//                    |> List.sortBy (fun (path,xyList) -> 
//                        let area =
//                            [1 .. (xyList.Length-2)]
//                            |> List.map (fun i -> ((snd xyList.[i])+(snd xyList.[i+1]))*((fst xyList.[i+1])-(fst xyList.[i]))/2.)
//                            |> List.sum
//                        area)
//                    |> List.mapi (fun i (s,(xy)) -> (i,s,(xy)))
//                let grid (data': (float*float) list ) =
//                    [|0. .. 0.03 .. 1.|]
//                    |> Array.map (fun x0 -> 
//                        let (x1,y1) = 
//                            data'
//                            |> List.rev
//                            |> List.filter (fun (x,y) -> x<=x0)
//                            |> List.last
//                        let (x2,y2) =
//                            data'
//                            |> List.rev
//                            |> List.filter (fun (x,y) -> x>x0)
//                            |> List.head 
//                        (y1-y2)/(x1-x2)*x0+(y2-(y1-y2)/(x1-x2)*x2)) // something....
//                let surface =
//                    Chart.Surface ((sortedData |> List.map (fun (i,s,xy) -> grid xy.Tail)), Opacity=0.8, Colorscale=StyleParam.Colorscale.Portland) //StyleParam.Colorscale.Hot
//                let line =
//                    let points =
//                        sortedData
//                        |> List.map (fun (i,s,xy) -> ((fst xy.Head)*30.,i,(snd xy.Head)))
//                    let labels =
//                        sortedData
//                        |> List.map (fun (i,s,xy) -> s)
//                    Chart.Scatter3d (points,StyleParam.Mode.Lines_Markers,Color="#2ca02c",Labels=labels)
//                [surface;line]
//                |> Chart.Combine
//                |> Chart.withX_AxisStyle("Dissimilarity(*30)")
//                |> Chart.withY_AxisStyle("Path")
//                |> Chart.withZ_AxisStyle("Complexity")
//                |> Chart.withSize(1000.,1000.)
//                |> Chart.Show

//            // Calculate line intersection
//            let calcIntersection (a:(float*float)) (b:(float*float)) (c:(float*float)) (d:(float*float)) =
//                let (Ax,Ay),(Bx,By),(Cx,Cy),(Dx,Dy) =
//                    (a,b,c,d)
//                let d = (Bx-Ax)*(Dy-Cy)-(By-Ay)*(Dx-Cx)  

//                if  d = 0. then
//                // parallel lines ==> no intersection in euclidean plane
//                    None
//                else
//                    let q = (Ay-Cy)*(Dx-Cx)-(Ax-Cx)*(Dy-Cy) 
//                    let r = q / d
//                    let p = (Ay-Cy)*(Bx-Ax)-(Ax-Cx)*(By-Ay)
//                    let s = p / d

//                    if r < 0. || r > 1. || s < 0. || s > 1. then
//                        None // intersection is not within the line segments
//                    else
//                        Some((Ax+r*(Bx-Ax)), (Ay+r*(By-Ay)))  // Px*Py

//            let intersect_ST_and_MM_lines (stPoint: float*float) (mmPoints: (float*float) list) =
//                let (a),(b) = ((0.,0.),((fst stPoint)/(snd stPoint),1.))
//                mmPoints
//                |> List.pairwise
//                |> List.map (fun (x,y) -> calcIntersection x y a b)

//            /// return two values: (dist to MM line * dist to ST point)
//            let getDistFromZero stPoint mmPoints =
//                (intersect_ST_and_MM_lines stPoint mmPoints)
//                |> List.find (fun i -> Option.isSome i)
//                |> Option.get
//                |> (fun (x,y) -> 
//                                    (
//                                    weightedEuclidean None [x;y] [0.;0.],                       // dist to SO line
//                                    weightedEuclidean None [fst stPoint;snd stPoint] [0.;0.])   // dist to DO point
//                                    )

//            ////
    
//            /// draw dissimilarity VS complexity plot
//            let drawDCsinglePath weightL items mmTree stTree =
//                let matrixPath = 
//                    items
//                    |> Array.sortBy (fun i -> i.ID)
//                    |> distMatrixWeightedOf distanceMatrixWeighted (weightL)

//                let pointDO = 
//                    pointDxC matrixPath (stTree |> Tree.filterLeaves)

//                let pointsSO =
//                    dxc_points_fromMM_Fn matrixPath mmTree

//                let normalize (maxDissim: float) (input: float*float) =
//                    let (x,y) = input
//                    (x/maxDissim,y)
    
//                let maxDiss = 
//                    pointsSO
//                    |> List.map fst
//                    |> List.max

//                let pointDOnorm = 
//                    normalize maxDiss pointDO  

//                let pointSOnorm = 
//                    pointsSO
//                    |> List.map (fun i -> normalize maxDiss i) 

//                let intersectsDOxSO = intersect_ST_and_MM_lines pointDOnorm pointSOnorm
    
//                [drawComparePlot pointDOnorm (pointSOnorm);
//                Chart.Line [(0.0, 0.0); (intersectsDOxSO |> List.choose (fun i -> i) |> List.head)];
//                Chart.Line [(0.0, 0.0); pointDOnorm];
//                ]
//                |> Chart.Combine
//                |> Chart.withSize (500.,500.)
//                //|> Chart.Show

//            /// get the DxC measure for ST structure, normalized to the MM DxC points
//            let getDCmeasure items mmTree stTree =
//                let matrixPath = 
//                    items
//                    |> distMatrixWeightedOf distanceMatrixWeighted (None)

//                let pointsSO =
//                    dxc_points_fromMM_Fn matrixPath mmTree

//                let normalize (maxDissim: float) (input: float*float) =
//                    let (x,y) = input
//                    (x/maxDissim,y)
    
//                let maxDiss = 
//                    pointsSO
//                    |> List.map fst
//                    |> List.max 

//                stTree 
//                |> Tree.filterLeaves
//                |> pointDxC matrixPath 
//                |> normalize maxDiss
//                |> (fun (x,y) -> sqrt(x*x+y*y))

//        module StatAnalysis =

//            /// complexity ratio as a division between all subbins in MMO structure and leaves in SST structure
//            let complexityRatio mmoTree sstTree :float =
//                let sst = sstTree |> Tree.filterLeaves |> Array.length |> float
//                let mmo = mmoTree |> Tree.mapTreeBins |> List.length |> float
//                mmo/sst

//            /// dubious meaning
//            let dissimilarityGain_max mmoTree sstTree =
//                let getMaxAverStd (tree: Node<string, Item>) =
//                    tree 
//                    |> Tree.mapTreeMembers 
//                    |> List.groupBy fst 
//                    |> List.map (fun (depth, nodes) -> 
//                        let points = 
//                            nodes 
//                            |> List.map (fun (_,members) -> 
//                                let vectors = members |> Array.map (fun x -> x.dataL)
                        
//                                vectors
//                                |> Array.allPairs vectors
//                                |> Array.map (fun (x,y) -> weightedEuclidean None x y))
//                            |> List.toArray
//                            |> Array.concat
//                        (depth, (points |> Array.max, points |> Array.average, points |> stDev )))
//                let sst = getMaxAverStd sstTree |> List.sortBy fst
//                let mmo = getMaxAverStd mmoTree |> List.sortBy fst
//                let minN = (min sst.Length mmo.Length) - 1
//                List.zip (sst.[0 .. minN]) (mmo.[0 .. minN]) 
//                |> List.map (fun ((d1,(max1,aver1,sd1)),(_,(max2,aver2,sd2))) -> (d1,(max1-max2),(aver1-aver2),(max sd1 sd2)))

//            /// draw a histogram // or spline for frequency plot
//            let drawFreqPlot bin (points: float []) =
//                let bw = 
//                    match bin with
//                    |Some x -> x
//                    |None -> FSharp.Stats.Distributions.Bandwidth.nrd0 (points)

//                let temporal = 
//                    points
//                    |> Distributions.Frequency.create bw
//                    //|> Distributions.Empirical.ofHistogram
//                    |> Map.toArray

//                Chart.Column temporal//Spline temporal

//        module Write =

//            /// create an output file with final group name for each input item
//            let allPathsWrite outputFile bins items shape =
        
//                bins
//                |> Array.map (fun n ->
                    
//                                let itemsOfN =
//                                    items
//                                    |> Array.filter (fun i -> i.BinL.Length>0 && i.BinL.[0] = n)
//                                    |> Array.mapi (fun index i -> {i with ID=index; dataL = i.dataL |> General.zScoreTransform })

//                                let treeMMofN = readMM  items.Length itemsOfN
//                                let stopwatch = new System.Diagnostics.Stopwatch()
//                                stopwatch.Start()
//                                let treeSTofN = Main.getST items.Length itemsOfN shape            //// change here which ST aproach to use
//                                let time = sprintf "SSN tree calculated in %f s since start" (stopwatch.Elapsed.TotalSeconds)
//                                stopwatch.Stop()
//                                let dc = DxC.getDCmeasure itemsOfN treeMMofN treeSTofN
//                                let fileName = sprintf "Bin_%s" n
//                                let title = sprintf "bin: %s, items: %i" n itemsOfN.Length
//                                let dcText = sprintf "DxC measure: %f" dc
//                                let header = "Bin\tItem"
//                                let content = 
//                                    treeSTofN 
//                                    |> Tree.mapTreeToBinsMembers 
//                                    |> Array.toList 
//                                    |> List.map (fun (bin,x) -> sprintf "%s\t%A" (String.Join(".", bin)) x.ProteinL)
                        
//                                File.WriteAllLines((sprintf "%s%s.txt" outputFile fileName), title :: time :: dcText :: header :: content)
//                                )

//    module GePhi =

//        open Main
//        open Types

//        type NodeParam =
//            {
//            ProteinIDs: string;
//            NodeBin: string;
//            LevelDepth: int;
//            Size: float;
//            Elements: int;
//            groupGain: float;
//            stepGain: float;
//            confGain: float;
//            X_Position: float;
//            Y_Position: float
//            }

//        /// convert tree structure to nodes and edges and send them to GePhi Streaming
//        let sendToGephiFromTreeParam (tree: Node<string, Item>) =
    
//            let rec toMemberSeq key depth (tree: Node<string,Item>) =
//                let fillNodeFrom (items: Item []) gGain sGain cGain : NodeParam =
//                    {
//                    ProteinIDs = String.Join(";", (General.groupIDFn items));
//                    NodeBin = String.Join(".",Array.append items.[0].BinL.[0 .. (depth-1)] [|key|]);
//                    LevelDepth = depth;
//                    Size = sqrt (float items.Length);//(float items.Length)/2.;
//                    Elements = items.Length;
//                    groupGain = gGain;
//                    stepGain = sGain;
//                    confGain = cGain;
//                    X_Position = 0.;
//                    Y_Position = 0.;
//                    }
//                [ 
//                match tree.Children with
//                |x when x=Map.empty -> 
//                            yield fillNodeFrom tree.Members tree.MaxGain tree.StepGain (fst tree.ConfGain) 
                        
//                |_  ->      
//                            yield fillNodeFrom tree.Members tree.MaxGain tree.StepGain (fst tree.ConfGain) 
//                            for child in tree.Children do                                        
//                                yield! toMemberSeq child.Key (depth+1) child.Value 
//                ]

//            let key = tree.Members.[0].BinL.[0]
//            let flatTree = toMemberSeq key 0 tree

//            let yList =
//                let plus index acc =
//                    if (flatTree.[index].LevelDepth>flatTree.[index-1].LevelDepth) then acc
//                    else (acc + 2. + (sqrt (float flatTree.[index-1].Elements)) + (sqrt (float flatTree.[index].Elements)))//(acc + 4. + (float flatTree.[index-1].Elements)/2. + (float flatTree.[index].Elements)/2.)
//                let rec loop index acc =
//                    match index with
//                    |0 -> acc
//                    |_ -> loop (index-1) (plus index acc) 
//                [for index in [0..(flatTree.Length-1)] do yield loop index 0.0]

//            let xList =
//                let maxDepth = 
//                    flatTree
//                    |> List.maxBy (fun i -> i.LevelDepth)
//                    |> (fun i -> i.LevelDepth)
//                let count level =
//                    flatTree
//                    |> List.filter (fun i -> i.LevelDepth=level)
//                    |> List.maxBy (fun i -> i.Elements)
//                    |> (fun i -> i.Elements)
//                    |> float |> sqrt
            
//                let rec loop index acc = 
//                    match index with
//                    |0 -> acc
//                    |_ -> loop (index-1) (acc + 10. + (count index) + (count (index-1)))
//                [for level in [0 .. maxDepth] do yield loop level 0.0]

//            let nodes = 
//                flatTree
//                |> List.mapi (fun i node -> 
//                                            (i, {node with 
//                                                    X_Position = xList.[node.LevelDepth];
//                                                    Y_Position = yList.[i]
//                                                    }))
//            let edges = 
//                flatTree 
//                |> List.tail
//                |> List.mapi ( fun i node -> 
//                                        let source = 
//                                            i - (flatTree 
//                                            |> List.cutAfterN (i+1)
//                                            |> fst
//                                            |> List.rev
//                                            |> List.findIndex (fun nodeBefore -> nodeBefore.LevelDepth < node.LevelDepth)) 
//                                        let target = i+1
//                                        let level = node.LevelDepth
//                                        let gain = node.groupGain
//                                        (source,target,(level,gain)))

//            let converterNode ((index,param) : int*NodeParam) =
//                [
//                Grammar.Attribute.Label param.NodeBin;
//                Grammar.Attribute.UserDef ("Level", param.LevelDepth);
//                Grammar.Attribute.UserDef ("Elements", param.Elements);
//                Grammar.Attribute.UserDef ("Group_Gain", System.Math.Round(param.groupGain,4));
//                Grammar.Attribute.UserDef ("Step_Gain", System.Math.Round(param.stepGain,4));
//                Grammar.Attribute.UserDef ("Conf_Gain", System.Math.Round(param.confGain,4));
//                Grammar.Attribute.UserDef ("Protein_IDs", param.ProteinIDs);
//                Grammar.Attribute.Size param.Size; 
//                Grammar.Attribute.PositionX param.X_Position;
//                Grammar.Attribute.PositionY param.Y_Position
//                ]

//            let converterEdge (edge : int*int*(int*float)) =
//                match edge with
//                |(_, _, c) -> [Grammar.Attribute.UserDef ("Level", fst c); Grammar.Attribute.UserDef ("Group_Gain", snd c);]

//            let postNodes = 
//                nodes 
//                |> List.map (fun a -> Streamer.addNode converterNode (fst a) a)

//            let postEdges = 
//                edges 
//                |> List.mapi (fun i (source,target,att) -> Streamer.addEdge converterEdge i (source) (target) (source,target,att))

//            [
//            postNodes;
//            postEdges
//            ]

//    module Plots =

//        open Types
//        open Analysis

//        //// colours
//        let colorBlue = "rgba(91,155,213,1)"
//        let colorOrange = "rgba(237,125,49,1)"
//        let colorGray = "rgba(165,165,165,1)"
//        let colorYellow = "rgba(255,192,0,1)"
//        let colorGreen = "rgba(112,173,71,1)"
//        let colorGrayBlue = "rgba(68,84,106,1)"
//        let colorBrightBlue = "rgba(68,114,196,1)"

//        let randomColor (r: Random) =
//            let (x,y,z) = (r.Next(0,256),r.Next(0,256),r.Next(0,256))
//            sprintf "rgba(%i,%i,%i,1)" x y z

//        /// draw a histogram, with given bin (optional) and a vertical line, defined by position on 0X and label (optional)
//        let drawHistogram name (bin: float option) (point: (float*string) option) data =
//            let bw = 
//                match bin with
//                |Some x -> x
//                |None -> FSharp.Stats.Distributions.Bandwidth.nrd0 (data |> Array.ofList)

//            let temporal = 
//                data
//                |> Distributions.Frequency.create bw 
//                |> Distributions.Frequency.getZip

//            let drawPoint =
//                match point with
//                |Some (v,anno) -> 
//                        let max =  
//                            temporal
//                            |> Seq.toList
//                            |> List.maxBy snd 
//                        ([(v, float (snd max));(v, 0.0)], anno)
//                |None -> ([],"")

//            let hist = Chart.Column (temporal, name)
//            let line = Chart.Line (fst drawPoint, snd drawPoint, Labels = [snd drawPoint; ""])
//            [hist;line] 
//            |> Chart.Combine 
//            |> Chart.withTraceName name


//        /// draw proteins kinetic as lines
//        let drawKinetik (data: Item []) time title =
    
//            let dataLine anno (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip time data
//                Chart.Line (points, Name = (sprintf "%i" protein.ID), Labels = [anno;"";"";"";"";""])
        
//            data 
//            |> Array.map (fun i -> dataLine "" i)
//            |> Chart.Combine
//            |> Chart.withTitle title
//        //    |> Chart.Show

//        /// draw proteins kinetic as lines
//        let drawKinetikTitle (data': Node<string,Item>) (binList : string list) time =
//            let data = data' |> Tree.findNodeMembers binList
//            let dataLine anno (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip time data
//                Chart.Line (points, Name = (sprintf "%i" protein.ID), Labels = [anno;"";"";"";"";""])
         
//            data 
//            |> Array.map (fun i -> dataLine "" i)
//            |> Chart.Combine
//            |> Chart.withTitle (String.Join(".", binList))
//        //    |> Chart.withSize (600,400)
//        //    |> Chart.ShowWPF

//        /// draw proteins kinetic as lines with given length
//        let drawKinetikTime (data: Item []) (timeLine: float [])=
    
//            let dataLine anno (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip timeLine data
//                Chart.Line (points, Name = (sprintf "p%i: bin%A" protein.ID protein.BinL), Labels = [anno;"";"";"";"";""])
    
//            data 
//            |> Array.map (fun i -> dataLine "" i)
//            |> Chart.Combine


//        /// draw proteins kinetic as a rangePlot with mean line, for the set except given special lines (optional), 
//        ///specified by index in a set of items and a label for the line
//        let drawKinetikRange time title (data: Item [] [])  =

//            let col c =
//                    match c with
//                    |0 -> colorBlue         //"rgba(91,155,213,1)"
//                    |1 -> colorOrange       //"rgba(237,125,49,1)"
//                    |2 -> colorGrayBlue     //"rgba(68,84,106,1)"       colorGray         //"rgba(165,165,165,1)"   
//                    |3 -> colorGreen        //"rgba(112,173,71,1)"
//                    |4 -> "rgba(180,0,0,1)"
//                    |5 -> colorYellow       //"rgba(255,192,0,1)"
//                    |6 -> "rgba(0,180,0,1)"
//                    |7 -> "rgba(180,0,180,1)"
//                    |8 -> colorBrightBlue   //"rgba(68,114,196,1)"
//                    |9 -> "rgba(0,0,180,1)"
//                    |10 -> "rgba(0,180,180,1)"
//                    |_ -> "rgba(180,180,0,1)"

//            let dataLine c (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip time data
//                let name = string protein.ID
//                Chart.Line (points, Name = name, Color= col c)
    
//            let dataLineAsRange c (proteins: Item [])  =
//                let (mean,min,max) = 
//                    proteins 
//                    |> Array.map (fun protein -> protein.dataL) 
//                    |> JaggedArray.transpose 
//                    |> Array.map (fun timepoint -> (Array.average timepoint, Array.min timepoint, Array.max timepoint))
//                    |> Array.unzip3

//                let cS = col c
//                let cT = cS |> String.replace ",1)" ",0.5)"

//                let meanTime = Array.zip time mean 
//                let rangePlot = Chart.Range (meanTime, min, max, Color=cS, RangeColor=cT)
//                [|rangePlot; Chart.Line (meanTime, Color=cS)|]

//            let (rangeData, specialData) = 
//                data
//                |> Array.indexed
//                |> Array.partition (fun  (id,i) -> i.Length>1)
        

//            let range =
//                rangeData
//                |> Array.map (fun (id, iA) -> dataLineAsRange id iA)
//                |> Array.concat

//            let specialLines =
//                specialData
//                |> Array.map (fun (id, proteinA) -> dataLine id proteinA.[0])

//            [range; specialLines] 
//            |> Array.concat 
//            |> Chart.Combine 
//            |> Chart.withTraceName title 

//        /// draw proteins kinetic as a rangePlot with mean line, for the set except given special lines (optional), 
//        ///specified by index in a set of items and a label for the line
//        let drawKinetikRangeRandomColor time title (data: Item [] [])  =
//            let r = new Random()
//            let dataLine label (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip time data
//                let name = string protein.ID
//                Chart.Line (points, Name = name)//, Labels = [label;"";"";"";"";""])
    
//            let dataLineAsRange anno (proteins: Item [])  =
//                let (mean,min,max) = 
//                    proteins 
//                    |> Array.map (fun protein -> protein.dataL) 
//                    |> JaggedArray.transpose 
//                    |> Array.map (fun timepoint -> (Array.average timepoint, Array.min timepoint, Array.max timepoint))
//                    |> Array.unzip3

        
//                let (x,y,z) = (r.Next(0,256),r.Next(0,256),r.Next(0,256))
//                let cS = sprintf "rgba(%i,%i,%i,1)" x y z
//                let cT = sprintf "rgba(%i,%i,%i,0.5)" x y z

//                let meanTime = Array.zip time mean 
//                let rangePlot = Chart.Range (meanTime, min, max, Color=cS, RangeColor=cT)//, Labels = [anno;"";"";"";"";""])
//                [|rangePlot; Chart.Line (meanTime, Color=cS)|]

//            let specialData = 
//                data
//                |> Array.filter (fun i -> i.Length=1)
//                |> Array.concat

//            let range =
//                data
//                |> Array.filter (fun i -> i.Length>1)
//                |> Array.map (dataLineAsRange "")
//                |> Array.concat

//            let specialLines =
//                specialData
//                |> Array.map (fun protein -> dataLine (string protein.ID) protein)

//            [range; specialLines] 
//            |> Array.concat 
//            |> Chart.Combine 
//            |> Chart.withTraceName title 

//        ///specified by index in a set of items and a label for the line
//        let drawKinetikRangeStack time title (data: Item [] [])  =

//            let minY = data |> Array.map (Array.map (fun i -> i.dataL |> Array.min) >> Array.min) |> Array.min
//            let maxY = data |> Array.map (Array.map (fun i -> i.dataL |> Array.max) >> Array.max) |> Array.max

//            let dataLine col (protein: Item)  =
//                let data = protein.dataL
//                let points = Array.zip time data
//                let name = string protein.ID
//                Chart.Line (points, Name = name, Color=col)
    
//            let dataLineAsRange col (proteins: Item []) =
//                let (mean,min,max) = 
//                    proteins 
//                    |> Array.map (fun protein -> protein.dataL) 
//                    |> JaggedArray.transpose 
//                    |> Array.map (fun timepoint -> (Array.average timepoint, Array.min timepoint, Array.max timepoint))
//                    |> Array.unzip3

//                let meanTime = Array.zip time mean 
//                let rangePlot = 
//                    Chart.Range (time, mean, min, max, Name = sprintf "%s-%A" (string proteins.[0].ID) (proteins.[0].BinL.[proteins.[0].BinL.Length-1]) , Color=col) 
//                    |> Chart.withLineStyle (Color=col, Dash=StyleParam.DrawingStyle.Solid)
//                let meanLine = Chart.Line (time, mean, Color=col)
//                [rangePlot; meanLine] 
//                |> Chart.Combine 
//                |> Chart.withY_AxisStyle ("", (minY,maxY), Showline=true, Showgrid=false) 
//                |> Chart.withX_AxisStyle ("", (time.[0],time.[time.Length-1]), Showline=false, Showgrid=false)

//            let singletons =
//                data
//                |> Array.filter (fun i -> i.Length=1)
//                |> Array.mapi (fun c protein -> 
//                    let col =
//                        match c with
//                        |0 -> "rgba(0,0,0,1)"   
//                        |1 -> colorOrange
//                        |2 -> colorBlue   
//                        |3 -> colorGreen 
//                        |4 -> colorYellow 
//                        |5 -> "rgba(0,180,0,1)"
//                        |6 -> "rgba(0,180,180,1)"
//                        |7 -> "rgba(180,0,180,1)"
//                        |8 -> "rgba(180,180,0,1)"
//                        |9 -> "rgba(0,0,180,1)"
//                        |10 -> "rgba(180,0,0,1)"
//                        |_ -> "rgba(0,0,0,1)"
//                    dataLine col protein.[0]
//                )
//                |> Chart.Combine 
//                |> Chart.withY_AxisStyle ("", (minY,maxY), Showline=true, Showgrid=false)
//                |> Chart.withX_AxisStyle ("", (time.[0],time.[time.Length-1]), Showline=false, Showgrid=false)

//            let range =
//                data
//                |> Array.filter (fun i -> i.Length>1)
//                |> Array.mapi (fun c list ->
//                    let col =
//                        match c with
//                        |0 -> "rgba(0,0,0,1)"
//                        |1 -> colorOrange  
//                        |2 -> colorBlue   
//                        |3 -> colorGreen 
//                        |4 -> colorYellow 
//                        |5 -> "rgba(0,180,0,1)"
//                        |6 -> "rgba(0,180,180,1)"
//                        |7 -> "rgba(180,0,180,1)"
//                        |8 -> "rgba(180,180,0,1)"
//                        |9 -> "rgba(0,0,180,1)"
//                        |10 -> "rgba(180,0,0,1)"
//                        |_ -> "rgba(0,0,0,1)"
//                    dataLineAsRange col list
//                    )

//            [|range;[|singletons|]|] 
//            |> Array.concat 
//            |> Chart.Stack 2
//            //|> Chart.Combine 
//            |> Chart.withTraceName title 

//        let drawLeavesRandomColor title recordPoints (tree: Types.Node<string,Types.Item>) =
//            let r = new Random()
//            tree
//            |> Tree.filterLeaves
//            |> (fun x -> x.[0 ..])
//            |> Array.map (fun list ->
//                let c = randomColor r
//                list |> Array.map (fun d -> Chart.Line(recordPoints, d.dataL, sprintf "%i" d.ID, Color=c) ) |> Chart.Combine
//                        )
//            |> Chart.Combine
//            |> Chart.withTitle title
//            |> Chart.withSize (600.,400.)

//        let drawLeaves title recordPoints (tree: Types.Node<string,Types.Item>) =
//            tree
//            |> Tree.filterLeaves
//            |> (fun x -> x.[0 ..])
//            |> Array.mapi (fun c list ->
//                let col =
//                    match c with
//                    |0 -> colorBlue         //"rgba(91,155,213,1)"
//                    |1 -> colorOrange       //"rgba(237,125,49,1)"
//                    |2 -> colorGrayBlue     //"rgba(68,84,106,1)"       colorGray         //"rgba(165,165,165,1)"   
//                    |3 -> colorGreen        //"rgba(112,173,71,1)"
//                    |4 -> "rgba(180,0,0,1)"
//                    |5 -> colorYellow       //"rgba(255,192,0,1)"
//                    |6 -> "rgba(0,180,0,1)"
//                    |7 -> "rgba(180,0,180,1)"
//                    |8 -> colorBrightBlue   //"rgba(68,114,196,1)"
//                    |9 -> "rgba(0,0,180,1)"
//                    |10 -> "rgba(0,180,180,1)"
//                    |_ -> "rgba(180,180,0,1)"
//                list |> Array.map (fun d -> Chart.Line(recordPoints, d.dataL, sprintf "%i" d.ID, Color=col) ) |> Chart.Combine
//                        )
//            |> Chart.Combine
//            |> Chart.withTitle title
//            |> Chart.withSize (600.,400.)

//        ///// DxC
//        let drawComparePlot matrixPath pointSSN (pointsMMO: (float*float) list) (clustersK: (Item [] []) list)  =
    
//            let normalize (maxDissim: float) (input: float*float) =
//                let (x,y) = input
//                (x/maxDissim,y)
        
//            let maxDiss = 
//                pointsMMO
//                |> List.map fst
//                |> List.max

//            let pointSSNnorm = 
//                normalize maxDiss pointSSN  

//            let pointMMOnorm = 
//                pointsMMO
//                |> List.map (fun i -> normalize maxDiss i) 

//            let minMMO =
//                pointMMOnorm
//                |> List.minBy (fun i -> General.weightedEuclidean None [0.;0.] [fst i; snd i])
    
//            let pointsHC =
//                clustersK
//                |> List.map (DxC.pointDxC matrixPath)
//                |> List.map (fun i -> normalize maxDiss i) 

//            let minHC =
//                pointsHC
//                |> List.minBy (fun i -> General.weightedEuclidean None [0.;0.] [fst i; snd i])

//            let doP = 
//                Chart.Point ([pointSSNnorm], Name = "SSN")
    
//            let soP =
//                let anno = [0 .. (pointMMOnorm.Length-1)] |> List.map string
//                Chart.Line (pointMMOnorm, Name = "MMO", Labels = anno, ShowMarkers=true)

//            let hcP =
//                let anno = [0 .. (pointsHC.Length-1)] |> List.map string
//                Chart.Line (pointsHC, Name = "Hierarchical clustering", Labels = anno, ShowMarkers=true)
   
//            [soP |> Chart.withLineStyle (Color=colorBlue);
//            doP |> Chart.withLineStyle (Color=colorOrange);
//            hcP |> Chart.withLineStyle (Dash = StyleParam.DrawingStyle.Dash, Color=colorGray);
//            Chart.Line [(0.0, 0.0); pointSSNnorm] |> Chart.withLineStyle (Color=colorOrange);
//            Chart.Line [(0.0, 0.0); minMMO] |> Chart.withLineStyle (Color=colorBlue);
//            Chart.Line [(0.0, 0.0); minHC] |> Chart.withLineStyle (Color=colorGray);
//            ]
//            |> Chart.Combine
//            |> Chart.withLegend (false)
//            |> Chart.withX_AxisStyle ("Purity, norm", Showgrid=false)
//            |> Chart.withY_AxisStyle ("Purity, norm", Showgrid=false)
//            |> Chart.withSize (500.,500.)
//            |> Chart.Show


