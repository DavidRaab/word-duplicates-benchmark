open System.Text.RegularExpressions

let rx = Regex(@"\w+", RegexOptions.Compiled)

let splitIntoWords str = [|
    let matches = rx.Matches(str)
    for m in matches -> m.Value
|]

// A lot Faster -- 2x - 4x -- But not quiet right
let splitIntoWords' (str:string) =
    str.Split(' ');

// Count words by using a Map
let wordCount words =
    Seq.fold (fun count word ->
        count |> Map.change word (function
            | None   -> Some 1
            | Some x -> Some (x+1)
        )
    ) Map.empty words

// Takes a wordCount map, and only returns the ones that occur more than 1 times
let onlyDuplicates wordCount =
    let folder state key value =
        if   value > 1
        then key :: state
        else state
    Map.fold folder [] wordCount

// Another implementation
let onlyDuplicates' wordCount =
    wordCount
    |> Map.filter (fun key value -> value > 1)
    |> Map.toList
    |> List.map fst

// A more abstract alternative to change a value in a map
// Either inserts Key/Value into map, if key is not present.
// Or reads previous value of key, and gives you change to combine old and new value
// Example:
//   This lookups key "Foo" and either insert "1" or add "1" to the already present value
//      map |> addCombine (fun ov nv -> ov + nv) "Foo" 1
let inline addCombine combine (key:'Key) (value:'Value) map =
    Map.change key (function
        | None   -> Some value
        | Some x -> Some (combine x value)
    ) map

// Returns a key from a dictionary or the default
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

// Map ListComp
let sol1 text =
    [for word in (wordCount (splitIntoWords text)) do
        if word.Value > 1 then word.Key]

// Map fold
let sol2 text =
    onlyDuplicates (wordCount (splitIntoWords text))

// Map chain
let sol3 text =
    onlyDuplicates' (wordCount (splitIntoWords text))

// CountBy
let sol4 text =
    splitIntoWords text
    |> Seq.countBy id
    |> Seq.filter (fun (_,x) -> x > 1)
    |> Seq.map fst
    |> Seq.toList

// CountBy Choose
let sol5 text =
    splitIntoWords text
    |> Seq.countBy id
    |> Seq.choose (fun (word,n) -> if n > 1 then Some word else None)
    |> Seq.toList

// CountBy List
let sol6 text =
    splitIntoWords text
    |> Seq.toList
    |> List.countBy id
    |> List.choose (fun (word,n) -> if n > 1 then Some word else None)

// ResizeArray
let sol7 text =
    let ra = ResizeArray<_>()
    let mutable lastAdded = ""
    let mutable previous  = ""
    for word in Seq.sort (splitIntoWords text) do
        if word <> lastAdded && word = previous then
            ra.Add word
            lastAdded <- word
        previous <- word
    Seq.toList ra

// addCombine
let sol8 text =
    splitIntoWords text |> Seq.fold (fun state word ->
        addCombine (+) word 1 state
    ) Map.empty
    |> Map.fold (fun state key value ->
        if   value > 1
        then key :: state
        else state
    ) []

// Dictionary
let sol9 text =
    let dic = System.Collections.Generic.Dictionary()

    for word in splitIntoWords text do
        dic.[word] <- (getValue 0 word dic) + 1

    [for KeyValue (key,value) in dic do
        if value > 1 then key]

// CountBy LC
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

// Mutable Array
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

// Helper for duplicateMut2 (Scan Array)
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

// Scan Array
let duplicateMut2 text =
    let result = System.Collections.Generic.HashSet<_>()
    let words  = splitIntoWords text

    for word in words do
        if contains2 word words then
            result.Add(word) |> ignore

    result

// Scan Array Full
let duplicateMut3 text =
    let result = System.Collections.Generic.HashSet<_>()
    let words  = splitIntoWords text

    for currentWord in words do
        if (Array.sumBy (fun word -> if word = currentWord then 1 else 0) words) > 1 then
            result.Add(currentWord) |> ignore

    result

// Array Only
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

    printfn ""
    printfn "Full Mutable Versions"

    benchPrint "Mutable Array"   2000 (fun () -> duplicateMut1 text)
    benchPrint "Scan Array"      1000 (fun () -> duplicateMut2 text)
    benchPrint "Scan Array Full" 1000 (fun () -> duplicateMut2 text)
    benchPrint "Array Only"      2000 (fun () -> duplicateMut4 text)

    0
