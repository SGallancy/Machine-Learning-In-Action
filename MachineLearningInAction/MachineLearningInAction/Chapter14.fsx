﻿// Chapter 14 covers SVD (Singular Value Decomposition)

#r @"..\..\MachineLearningInAction\packages\MathNet.Numerics.2.4.0\lib\net40\MathNet.Numerics.dll"
#r @"..\..\MachineLearningInAction\packages\MathNet.Numerics.FSharp.2.4.0\lib\net40\MathNet.Numerics.FSharp.dll"

open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra.Double
open MathNet.Numerics.Statistics

type Rating = { UserId: int; DishId: int; Rating: int }

// Our existing "ratings database"
let ratings = [
    { UserId = 0; DishId = 0; Rating = 2 };
    { UserId = 0; DishId = 3; Rating = 4 };
    { UserId = 0; DishId = 4; Rating = 4 };
    { UserId = 1; DishId = 10; Rating = 5 };
    { UserId = 2; DishId = 7; Rating = 1 };
    { UserId = 2; DishId = 0; Rating = 4 };
    { UserId = 3; DishId = 0; Rating = 3 };
    { UserId = 3; DishId = 1; Rating = 3 };
    { UserId = 3; DishId = 2; Rating = 4 };
    { UserId = 3; DishId = 4; Rating = 3 };
    { UserId = 3; DishId = 7; Rating = 2 };
    { UserId = 3; DishId = 8; Rating = 2 };
    { UserId = 4; DishId = 0; Rating = 5 };
    { UserId = 4; DishId = 1; Rating = 5 };
    { UserId = 4; DishId = 2; Rating = 5 };
    { UserId = 5; DishId = 6; Rating = 5 };
    { UserId = 5; DishId = 9; Rating = 5 };
    { UserId = 6; DishId = 0; Rating = 4 };
    { UserId = 6; DishId = 2; Rating = 4 };
    { UserId = 6; DishId = 10; Rating = 5 };
    { UserId = 7; DishId = 5; Rating = 4 };
    { UserId = 7; DishId = 10; Rating = 4 };
    { UserId = 8; DishId = 6; Rating = 5 };
    { UserId = 9; DishId = 3; Rating = 3 };
    { UserId = 9; DishId = 8; Rating = 4 };
    { UserId = 9; DishId = 9; Rating = 5 };
    { UserId = 10; DishId = 0; Rating = 1 };
    { UserId = 10; DishId = 1; Rating = 1 };
    { UserId = 10; DishId = 2; Rating = 2 };
    { UserId = 10; DishId = 3; Rating = 1 };
    { UserId = 10; DishId = 4; Rating = 1 };
    { UserId = 10; DishId = 5; Rating = 2 };
    { UserId = 10; DishId = 6; Rating = 1 };
    { UserId = 10; DishId = 8; Rating = 4 };
    { UserId = 10; DishId = 9; Rating = 5 } ]

// Let's populate a matrix with these ratings;
// unrated items are denoted by a 0

let rows = 11
let cols = 11
let data = DenseMatrix(rows, cols)
ratings 
|> List.iter (fun rating -> 
       data.[rating.UserId, rating.DishId] <- (float)rating.Rating)

// "Pretty" rendering of a matrix
// something is not 100% right here, but it's good enough
let printNumber v = 
    if v < 0. 
    then printf "%.2f " v 
    else printf " %.2f " v
// Display a Matrix in a "pretty" format
let pretty matrix = 
    Matrix.iteri (fun row col value ->
        if col = 0 then printfn "" else ignore ()
        printNumber value) matrix
    printfn ""

printfn "Original data matrix"
pretty data

// Now let's run a SVD on that matrix
let svd = data.Svd(true)
let U, sigmas, Vt = svd.U(), svd.S(), svd.VT()

// recompose the S matrix from the singular values
printfn "S-matrix, with singular values in diagonal"
let S = DiagonalMatrix(rows, cols, sigmas.ToArray())
pretty S

// The SVD decomposition verifies
// data = U x S x Vt

printfn "Reconstructed matrix from SVD decomposition"
let reconstructed = U * S * Vt
pretty reconstructed

// Can we interpret the SVD decomposition? Let's see.

// Each row maps to a User, 
// each column to an extracted Category
let userToCategory = U * S |> pretty

// Each row maps to an extracted Category, 
// each column to a Dish
let categoryToDish = S * Vt |> pretty

// We can use SVD as a data compression mechanism
// by retaining only the n first singular values

// Total energy is the sum of the squares 
// of the singular values
let totalEnergy = sigmas.DotProduct(sigmas)
// Let's compute how much energy is contributed
// by each singular value
printfn "Energy contribution by Singular Value"
sigmas.ToArray() 
|> Array.fold (fun acc x ->
       let energy = x * x
       let percent = (acc + energy)/totalEnergy
       printfn "Energy: %.1f, Percent of total: %.3f" energy percent
       acc + energy) 0. 
|> ignore

// We'll retain only the first 5 singular values,
// which cover 90% of the total energy
let subset = 5
let U' = U.SubMatrix(0, U.RowCount, 0, subset)
let S' = S.SubMatrix(0, subset, 0, subset)
let Vt' = Vt.SubMatrix(0, subset, 0, Vt.ColumnCount)

// Using U', S', Vt' instead of U, S, Vt
// should produce a "decent" approximation
// of the original matrix

printfn "Approximation of the original matrix"
U' * S' * Vt' |> pretty

// To make recommendations we need a similarity measure
type similarity = Generic.Vector<float> -> Generic.Vector<float> -> float

// Larger distances imply lower similarity
let euclideanSimilarity (v1: Generic.Vector<float>) v2 =
    1. / (1. + (v1 - v2).Norm(2.))

// Similarity based on the angle
let cosineSimilarity (v1: Generic.Vector<float>) v2 =
    v1.DotProduct(v2) / (v1.Norm(2.) * v2.Norm(2.))

// Similarity based on the correlation
let pearsonSimilarity (v1: Generic.Vector<float>) v2 =
    if v1.Count > 2 
    then 0.5 + 0.5 * Correlation.Pearson(v1, v2)
    else 1.

// Let's check if that works
// Retrieve the first 2 dishes from the data matrix
let dish0 = data.Column(0)
let dish1 = data.Column(1)

// A dish should be 100% similar to itself
printfn "Euclidean similarity: %.2f" (euclideanSimilarity dish0 dish0)
printfn "Cosine similarity: %.2f" (cosineSimilarity dish0 dish0)
printfn "Pearson similarity: %.2f" (pearsonSimilarity dish0 dish0)

// How similar are dish0 and dish1?
printfn "Euclidean sim., Dish 1 & 2: %.2f" (euclideanSimilarity dish0 dish1)
printfn "Cosine sim., Dish 1 & 2: %.2f" (cosineSimilarity dish0 dish1)
printfn "Pearson sim., Dish 1 & 2: %.2f" (pearsonSimilarity dish0 dish1)

// Naive recommendation

// Reduce 2 vectors to their non-zero pairs
let nonZeroes (v1:Generic.Vector<float>) 
              (v2:Generic.Vector<float>) =
    // Grab non-zero pairs of ratings 
    let size = v1.Count
    let overlap =
        [ 0 .. (size - 1) ] 
        |> List.fold (fun acc i -> 
            if v1.[i] > 0. && v2.[i] > 0. 
            then (v1.[i], v2.[i])::acc else acc) []
    // Recompose vectors if there is something left
    match overlap with
    | [] -> None
    | x  -> 
        let v1', v2' =
            x
            |> List.toArray
            |> Array.unzip
        Some(DenseVector(v1'), DenseVector(v2'))

// Retrieve rating of a dish and similarity with another dish
let weightedRating (unrated:Generic.Vector<float>) 
                   (rated:Generic.Vector<float>) 
                   (userId:int) (sim:similarity) =
    if unrated.[userId] > 0. then None // we have a rating already
    else
        let rating = rated.[userId]
        if rating = 0. then None // we have no rating to use
        else
            let overlap = nonZeroes unrated rated
            match overlap with
            | None -> None
            | Some(u, v) -> Some(rating, sim u v)

/// Compute weighted average of a sequence of (value, weight)
let weightedAverage (data: (float * float) seq) = 
    let weightedTotal, totalWeights = 
        Seq.fold (fun (R,S) (r, s) -> 
            (R + r * s, S + s)) (0., 0.) data
    if (totalWeights <= 0.) 
    then None 
    else Some(weightedTotal/totalWeights)

// Compute estimated rating for a dish not yet rated by user
let estimatedRating (sim:similarity) 
                    (data:Generic.Matrix<float>) 
                    (userId:int) (dishId:int) =
    let dish = data.Column(dishId)
    match (dish.[userId] > 0.) with
    | true -> None // dish has been rated already
    | false -> 
        let size = data.ColumnCount
        let ratings =
            [ 0 .. (size - 1) ]
            |> List.map (fun col -> (weightedRating dish (data.Column(col)) userId sim))
            |> List.choose id
        match ratings with
        | [] -> None
        | data -> weightedAverage data

/// data, row (user), column (item), similarity    
type estimator = Generic.Matrix<float> -> int -> int -> float option

/// Produce an ordered list of recommendations for a user,
/// given a data matrix and a rating estimator
let recommend (data:Generic.Matrix<float>) 
              (userId:int) 
              (ratingEstimator:estimator) =
    let size = data.ColumnCount
    [ 0 .. (size - 1) ] 
    |> List.map (fun dishId -> 
        dishId, ratingEstimator data userId dishId)
    |> List.filter (fun (dishId, rating) -> rating.IsSome)
    |> List.map (fun (dishId, rating) -> (dishId, rating.Value))
    |> List.sortBy (fun (dishId, rating) -> - rating)

// We create 3 simple estimators                        
let euclideanSimple:estimator = estimatedRating euclideanSimilarity
let cosineSimple:estimator = estimatedRating cosineSimilarity
let pearsonSimple:estimator = estimatedRating pearsonSimilarity

// Let's check some recommendations
let rec0 = recommend data 0 euclideanSimple
let rec1 = recommend data 1 cosineSimple
let rec2 = recommend data 2 pearsonSimple

// SVD-based recommendation

// Energy level
let energy = 0.9
let valuesForEnergy (min:float) (sigmas:Generic.Vector<float>) =
    let totalEnergy = sigmas.DotProduct(sigmas)
    let rec search i accEnergy =
        let x = sigmas.[i]
        let energy = x * x
        let percent = (accEnergy + energy)/totalEnergy
        match (percent >= min) with
        | true -> i
        | false -> search (i + 1) (accEnergy + energy)
    search 0 0.

// Retrieve rating of a dish and similarity with another dish
let svdWeightedRating (unrated:Generic.Vector<float>) (rated:Generic.Vector<float>) (unrated':Generic.Vector<float>) (rated':Generic.Vector<float>) (userId:int) (sim:similarity) =
    if unrated.[userId] > 0. then None // we have a rating already
    else
        let rating = rated.[userId]
        if rating = 0. then None // we have no rating to use
        else Some(rating, sim unrated' rated')

let svdRating (sim:similarity) (data:Generic.Matrix<float>) (userId:int) (dishId:int) =
    let dish = data.Column(dishId)
    match (dish.[userId] > 0.) with
    | true -> None // dish has been rated already
    | false ->         
        let svd = data.Svd(true)
        let U, sigmas, Vt = svd.U(), svd.S(), svd.VT()
        let subset = valuesForEnergy nrj sigmas
        // fix this, directly create S'
        let S = DiagonalMatrix(data.RowCount, data.ColumnCount, sigmas.ToArray())

        let U' = U.SubMatrix(0, U.RowCount, 0, subset)
        let S' = S.SubMatrix(0, subset, 0, subset)
        let Vt' = Vt.SubMatrix(0, subset, 0, Vt.ColumnCount)

        let data' = (data.Transpose() * U' * S').Transpose() 
        let dish' = data'.Column(dishId)
        let size = data'.ColumnCount
        // Incorrect: rating should be pulled from data, not data'
        let ratings =
            [ 0 .. (size - 1) ]
            |> List.map (fun col -> (svdWeightedRating dish (data.Column(col)) dish' (data'.Column(col)) userId sim))
            |> List.choose id
        match ratings with
        | [] -> None
        | data -> weightedAverage data

let euclideanSvd:estimator = svdRating euclideanSimilarity
let cosineSvd:estimator = svdRating cosineSimilarity
let pearsonSvd:estimator = svdRating pearsonSimilarity

[ 0 .. 10 ]
|> List.iter (fun userId ->
    printfn "User %i" userId
    [ 0 .. 10 ]
    |> List.iter (fun dishId ->
        let r1 = euclideanEstimator data userId dishId
        let r2 = cosineEstimator data userId dishId
        let r3 = pearsonEstimator data userId dishId
        let s1 = euclideanSvd data userId dishId
        let s2 = cosineSvd data userId dishId
        let s3 = pearsonSvd data userId dishId
        printfn "... dish %i: [Naive] eucl %.2A cos %.2A pear %.2A / [SVD] eucl %.2A cos %.2A pear %.2A" dishId r1 r2 r3 s1 s2 s3)
)


// create synthetic data

let person1 = [| 5.; 4.; 1.; 1.; 2.; 1.; 1.; 5. |]
let person2 = [| 1.; 1.; 4.; 5.; 1.; 5.; 4.; 1. |]
let person3 = [| 4.; 5.; 4.; 1.; 1.; 2.; 2.; 1. |] 
let person4 = [| 1.; 1.; 1.; 1.; 1.; 1.; 1.; 5. |] 
let person5 = [| 1.; 1.; 5.; 4.; 5.; 1.; 1.; 1. |] 

let templates = [|
    person1;
    person2;
    person3;
    person4;
    person5 |]

let rng = new System.Random()
let density = 0.5

let createPerson (template:float[]) =
    template 
    |> Array.map (fun x -> if rng.NextDouble() < density then x else 0.)

let rows2 = 1000
let cols2 = 8

let syntheticData = DenseMatrix(rows2, cols2)

let ts = [| for row in 0 .. (rows2 - 1) -> rng.Next(0, 5) |]

for row in 0 .. (rows2 - 1) do
    let template = templates.[ts.[row]]
    let fake = createPerson template
    for col in 0 .. (cols2 - 1) do
        syntheticData.[row, col] <- fake.[col]

recommend syntheticData 1 cosineSvd;;