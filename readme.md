<h1 align="center"> Project Report </h1> <br>


## Table of Contents

- [Table of Contents](#table-of-contents)
- [Build Process](#build-process)
- [What is Working](#what-is-working)
  - [Gossip](#gossip)
    - [Normal Scale](#normal-scale)
    - [Log Scale](#log-scale)
  - [PushSum](#pushsum)
    - [Linear Scale](#linear-scale)
    - [Logarithmic Scale](#logarithmic-scale)
- [What is the largest network you managed to deal with for each type of topology and algorithm](#what-is-the-largest-network-you-managed-to-deal-with-for-each-type-of-topology-and-algorithm)

## Build Process

- unzip the compressed file using `unzip filename.zip`
- `dotnet fsi --langversion:preview proj2.fsx nodeNum topology protocol` to run script where `nodeNum` is the number of nodes you want to run topology for. `topology` can have values in [`line`, `full`, `2D`, `Imp2D`]. protocol can have values either `gossip` or `push-sum`.

## What is Working

We able to run all `line`, `full`, `2D` and `Imp2D` in any combination with `gossip` or `push-sum` protocol. The convergence in Gossip protocol is achieve when all the nodes have converged. A node is set to be converged when it listens to message for 10th time. After convergence, the node stops transmiting message to it's either. Once the network is converged i.e. all nodes are converged, the total time for convergence is printed out.

### Gossip

#### Normal Scale

- Line topology is the slowest to converge. This is due to the fact that it has only access to 2 neighbors (left and right node).

- Full topology is fastest to converge in all possible scenario. This is because it is connected all the nodes and convergence is faster to achieve in this scenario

- As expected, 2D and imperfect 2D would achieve the convergence time in between line and full with imperfect 2D performing slightly better or equal to 2D performance.

![Gossip](./docs/gossip.png)

#### Log Scale

![Gossip Log Scale](./docs/gossip_log.png)

### PushSum



#### Linear Scale

![PushSum](./docs/pushsum.png)

#### Logarithmic Scale

![PushSum](./docs/pushsum_log.png)

## What is the largest network you managed to deal with for each type of topology and algorithm

The largest network that we have managed to solve is 10k nodes for each topology and algorithm.
