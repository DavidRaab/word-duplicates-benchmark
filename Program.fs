open System.Text.RegularExpressions

let rx = Regex(@"\w+", RegexOptions.Compiled)

let splitIntoWords str = [|
    let matches = rx.Matches(str)
    for m in matches -> m.Value
|]

// A lot Faster -- 2x - 4x -- But not quiet right
let splitIntoWords' (str:string) =
    str.Split(' ');

let wordCount words =
    Seq.fold (fun count word ->
        count |> Map.change word (function
            | None   -> Some 1
            | Some x -> Some (x+1)
        )
        // Map.change word (Some << Option.fold (+) 1) count
        // (Option.fold (fun state x -> Some (state.Value + x)) (Some 1))
        // (Option.map (fun x -> x+1) >> Option.orElse (Some 1)) 
    ) Map.empty words
    

let onlyDuplicates wordCount =
    let folder state key value =
        if   value > 1
        then key :: state
        else state
    Map.fold folder [] wordCount

let onlyDuplicates' wordCount =
    wordCount
    |> Map.filter (fun key value -> value > 1)
    |> Map.toList
    |> List.map fst

let inline addCombine combine (key:'Key) (value:'Value) map =
    Map.change key (function
        | None   -> Some value
        | Some x -> Some (combine x value)
    ) map

let getValue def key (dict:System.Collections.Generic.IDictionary<_,_>) =
    let mutable value = Unchecked.defaultof<_>
    if   dict.TryGetValue(key, &value)
    then value
    else def


// Benchmark Utilities
let timeit count code =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    for i=1 to count do
        code () |> ignore
    sw.Stop ()
    {| Amount = count; CallsPerSecond = float count / sw.Elapsed.TotalSeconds |}

let benchPrint (msg:string) count code =
    let time = timeit count code
    printfn "%-15s %6d: %6.1f/s" (msg.[0..14]) time.Amount time.CallsPerSecond


// Functions to Benchmark
let sol1 text =
    [for word in (wordCount (splitIntoWords text)) do
        if word.Value > 1 then word.Key]

let sol2 text =
    onlyDuplicates (wordCount (splitIntoWords text))

let sol3 text =
    onlyDuplicates' (wordCount (splitIntoWords text))

let sol4 text =
    splitIntoWords text
    |> Seq.countBy id
    |> Seq.filter (fun (_,x) -> x > 1)
    |> Seq.map fst
    |> Seq.toList

let sol5 text =
    splitIntoWords text
    |> Seq.countBy id
    |> Seq.choose (fun (word,n) -> if n > 1 then Some word else None)
    |> Seq.toList

let sol6 text =
    splitIntoWords text
    |> Seq.toList
    |> List.countBy id
    |> List.choose (fun (word,n) -> if n > 1 then Some word else None)

let sol7 text =
    let ra = ResizeArray<_>()
    let mutable lastAdded = ""
//    let mutable previous  = ""
    for word in Seq.sort (splitIntoWords text) do
        if word <> lastAdded then //&& word = previous then
            ra.Add word
            lastAdded <- word
//        previous <- word
    Seq.toList ra

let sol8 text =
    splitIntoWords text
    |> Seq.fold (fun state word ->
        addCombine (+) word 1 state
       ) Map.empty
    |> Map.fold (fun state key value ->
        if   value > 1 
        then key :: state
        else state
    ) []

let sol9 text =
    let dic = System.Collections.Generic.Dictionary()
    let get = getValue 1

    for word in splitIntoWords text do
        dic.[word] <- get word dic + 1
    
    [for KeyValue (key,value) in dic do
        if value > 1 then key]

let sol10 text =
    let wordCount = Seq.countBy id (splitIntoWords text)
    [for (word,count) in wordCount do
        if count > 1 then 
            yield word]


// Read input from file
let text = System.IO.File.ReadAllText("LoremIpsum.txt")

//  All Functions
let fns = [
    "Map ListComp"  , sol1 , 1000
    "Map fold"      , sol2 , 1000
    "Map chain"     , sol3 , 1000
    "CountBy"       , sol4 , 2000
    "CountBy Choose", sol5 , 2000
    "CountBy List"  , sol6 , 2000
    "ResizeArray"   , sol7 , 1500
    "addCombine"    , sol8 , 1000
    "Dictionary"    , sol9 , 2000
    "CountBy LC"    , sol10, 2000
]

// Full mutable versions
let duplicateMut1 text =
    let words = System.Collections.Generic.Dictionary<_,_>()
    for word in splitIntoWords text do
        let mutable count = 0
        if   words.TryGetValue(word, &count)
        then words.[word] <- count + 1
        else words.[word] <- 1
    
    let result = ResizeArray<_>()
    for word in words do
        if word.Value > 1 then
            result.Add word

    result

let inline contains2 element array =
    let mutable found = 0
    let max = Array.length array
    let rec loop i =
        if i < max then
            if array.[i] = element then
                found <- found + 1
                if found = 2 then 
                    true
                else 
                    loop (i+1)
            else
                loop (i+1)
        else
            false
    loop 0

let duplicateMut2 text =
    let result = System.Collections.Generic.HashSet<_>()
    let words  = splitIntoWords text

    for word in words do
        if contains2 word words then
            result.Add(word) |> ignore

    result

let duplicateMut3 text =
    let result = System.Collections.Generic.HashSet<_>()
    let words  = splitIntoWords text

    for currentWord in words do
        if (Array.sumBy (fun word -> if word = currentWord then 1 else 0) words) > 1 then
            result.Add(currentWord) |> ignore

    result

let duplicateMut4 text =
    splitIntoWords text
    |> Array.countBy id
    |> Array.filter (fun (_,count) -> count > 1)
    |> Array.map fst


[<EntryPoint>]
let main argv = 
    // Check if all Functions return the same
    let results =
        [for (_,f,count) in fns do
            System.String.Join(",", (Array.sort (List.toArray (f text))))]
    
    let isEqual = List.forall (fun res -> res = List.head results) (List.tail results)
    printfn "All Equal (should be true): %b" isEqual


    // Start Benchmarking
    printfn "Benchmarking..."
    for (msg,code,count) in fns do
        benchPrint msg count (fun () -> code text)

    benchPrint "Mutable Array"   2000 (fun () -> duplicateMut1 text)
    benchPrint "Scan Array"      1000 (fun () -> duplicateMut2 text)
    benchPrint "Scan Array Full" 1000 (fun () -> duplicateMut2 text)
    benchPrint "Array Only"      2000 (fun () -> duplicateMut4 text)

    0
