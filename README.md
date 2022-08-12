# Duplicates

In an online forum I had a discussion of the *fastest* way to get
only duplicated words of a text file. I said that a dictionary would
be the fastest version. At least, thats how I am used to solve this problem
in Perl.

All those "Enterprise" developers said a Dictionary would be slower,
because of hashing blablablabla...

So I created this Benchmark and added a lot of different versions to this
problem.

> Remember the task is not to get unique words. Its about **only** getting the
> words in a text file that apperas **more** than **once**!

## Running

Running this program produces an output like this on my machine.

    All Equal (should be true): true
    Benchmarking...
    Map ListComp      1000:  121.9/s
    Map fold          1000:  136.4/s
    Map chain         1000:  148.9/s
    CountBy           2000:  210.7/s
    CountBy Choose    2000:  192.3/s
    CountBy List      2000:  179.5/s
    ResizeArray       1500:  143.8/s
    addCombine        1000:  148.5/s
    Dictionary        2000:  205.0/s
    CountBy LC        2000:  181.0/s

    Full Mutable Versions -- Maximum Performance
    Mutable Array     2000:  203.5/s
    Scan Array        1000:  124.0/s
    Scan Array Full   1000:  123.2/s
    Array Only        2000:  195.9/s
    
## Interpretation of Results

1. **CountBy** is the fastest version. It just uses the built-in **Seq.countBy**
function. This btw. uses a **Dictionary** under the hood.

2. **Dictionary** is the version I would write if **CountBy** would not exists. Or
in other words, if I had to implement it myself. What was the task to begin with.
It uses a mutable dictionary for the interim data and then transforms it into
a list with List Comprehension.

3. **Map ListComp**, **Map fold**, **Map chain** are still the same algorithm, but
using a **Map** instead of a **Dictionary**.

4. **CountBy Choose** and **CountBy List** are minor changes to **CountBy**.

5. **ResizeArray** was one of the *more intelligent* solution someone suggested
instead of using a **Dictionary**. Split the words, sort it, then iterate through
the words. Keep track of the previous word, and the last added word to the result.
We add the word if we didn't added the word already and it is the same as previous
(so it appears at least twice).

6. **addCombine** is basically the same as **Map Fold** but uses the **addCombine**
helper function.

7. **CountBy LC** is the same as **CountBy** but uses List Comprehension instead
of Function Piping.

## *The Full Mutable* versions all use only Mutable Data-Structures.

All versions above return an immutable List.

1. **MutableArray** is the same as **CountBy** or **Dictionary**, it counts the
words with a **Dictionary** and then only picks duplicates and pushes it into
an **ResizeArray**.

2. **Scan Array** is the silly idea, to not use a **Dictionary** and to re-scan
the word **List** over and over again, and stop if at least two invocations was
found. Then we add it to a **HashSet**. This way we avoid adding the same word
to the result over and over again.

3. **Scan Array Full** is the same as **Scan Array** but just scans the whole array
without short circuiting.

4. **Array Only** is the same as **CountBy** but does all operations on an
an **Array** and retuns an **Array** instead of **List**.

## Final Verdict

If you execute the benchmark yourself you will get similar results. Some
operations are sometimes faster/slower on an invocation. It probably has to
do with Garbace Collection running.

But overall you get the idea that using a **Dictionary** is not slow. I don't
get it why .Net people avoid it so often. A Dictionary or Hash (Perl) is one
of the most used data-structure in Perl but also JavaScript, Python and so on.

Picking the right algorithm is more important than thinking Hashing a key
would be slow. If it would be slow, there would be no point in ever using a
**Dictionary** at all.

The solution also shows that immutability is in general not the biggest
performance impact. The Full Mutable version has no advantages over returning
an immutable list. But this also could be because List creation in F# is
very optimized.

Using **Map** as interim data is noticible slower. But this case is a good
example when you can use mutable data in an language in F#. As we never return
that **Map** data-structure we create in the **Map ...** versions, we can use
a **Dictionary** safely. The function can still be considered immutable.

## Regex Performance

On Performance. This task is in general incredible slow in .Net it has todo
with its regex Engine. If I do the same in Perl like the Dictionary I get
around **4-5 times faster** results!

You also get the same results if you change the `splitWords` function into
a function that just splits a string on a whitespace character. For Benchmarking
the different solution, you get a better understand of the difference between
the solution. The above solutions are so slow, because like 80%+ or more time
is spent in the Regex Engine.

Changing it into word Split, you get a better understanding of the performance
impact of the choosed algorithm. 

Just switch the `splitIntoWords` and `splitIntoWords'` functions with each other.

Now the task we are given are not exactly solved, as we get extra punctuations
and other garbage. 

But, it shows better the impact of the choosen Algorithm instead of benchmarking
the .Net Regex Engine.

    All Equal (should be true): false
    Benchmarking...
    Map ListComp      1000:  350.3/s
    Map fold          1000:  356.3/s
    Map chain         1000:  354.3/s
    CountBy           2000: 1019.6/s
    CountBy Choose    2000: 1019.0/s
    CountBy List      2000:  939.6/s
    ResizeArray       1500:  395.9/s
    addCombine        1000:  355.5/s
    Dictionary        2000: 1319.6/s
    CountBy LC        2000: 1029.4/s

    Full Mutable Versions
    Mutable Array     2000: 1376.5/s
    Scan Array        1000:  232.2/s
    Scan Array Full   1000:  232.4/s
    Array Only        2000: 1129.0/s


