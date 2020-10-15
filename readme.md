<h1 align="center"> Project Report </h1> <br>


## Table of Contents

- [Table of Contents](#table-of-contents)
- [Build Process](#build-process)
- [What is Working](#what-is-working)
- [What is the largest network you managed to deal with for each type oftopology and algorithm](#what-is-the-largest-network-you-managed-to-deal-with-for-each-type-oftopology-and-algorithm)

## Build Process

- unzip the compressed file using `unzip filename.zip`
- `dotnet fsi --langversion:preview proj2.fsx nodeNum topology protocol` to run script where `nodeNum` is the number of nodes you want to run topology for. `topology` can have values in [`line`, `full`, `2D`, `Imp2D`]. protocol can have values either `gossip` or `push-sum`.

## What is Working

We able to run all `line`, `full`, `2D` and `Imp2D` in any combination with `gossip` or `push-sum` protocol

## What is the largest network you managed to deal with for each type oftopology and algorithm

