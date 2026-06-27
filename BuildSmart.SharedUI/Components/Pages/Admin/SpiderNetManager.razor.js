let originalNodes = [];
let originalEdges = [];

export async function initializeGraph(container, nodesData, edgesData, dotNetHelper) {
    if (!container) return;

    // Clean up existing network instance to avoid duplicate canvas elements and broken click events
    if (container.network) {
        try {
            container.network.destroy();
        } catch (e) {
            console.error("Error destroying network:", e);
        }
        container.network = null;
    }

    if (!window.vis) {
        await new Promise((resolve) => {
            const script = document.createElement("script");
            script.src = "https://unpkg.com/vis-network/standalone/umd/vis-network.min.js";
            script.onload = resolve;
            document.head.appendChild(script);
        });
    }

    originalNodes = JSON.parse(JSON.stringify(nodesData));
    originalEdges = JSON.parse(JSON.stringify(edgesData));

    const nodes = new vis.DataSet(nodesData);
    const edges = new vis.DataSet(edgesData);

    const data = { nodes, edges };
    const options = {
        nodes: {
            shape: 'dot',
            size: 24,
            font: {
                size: 14,
                color: '#ffffff',
                strokeWidth: 3,
                strokeColor: '#1e1e2d'
            },
            borderWidth: 2,
            shadow: true
        },
        edges: {
            width: 2,
            shadow: true,
            arrows: {
                to: { enabled: true, scaleFactor: 1 }
            },
            color: {
                color: '#8c909a',
                highlight: '#512bd4'
            },
            font: {
                size: 11,
                color: '#ffffff',
                strokeWidth: 2,
                strokeColor: '#1e1e2d',
                align: 'middle'
            }
        },
        physics: {
            solver: 'forceAtlas2Based',
            forceAtlas2Based: {
                gravitationalConstant: -60,
                centralGravity: 0.015,
                springLength: 120,
                springConstant: 0.08
            }
        }
    };

    const network = new vis.Network(container, data, options);

    network.on("click", function (params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            dotNetHelper.invokeMethodAsync('OnNodeSelected', nodeId);
            filterGraphSelection(nodes, edges, [nodeId]);
        } else if (params.edges.length > 0) {
            const edgeId = params.edges[0];
            dotNetHelper.invokeMethodAsync('OnEdgeSelected', edgeId);
            const parts = edgeId && edgeId.split ? edgeId.split('_') : [];
            if (parts.length === 2) {
                filterGraphSelection(nodes, edges, [parts[0], parts[1]]);
            }
        } else {
            dotNetHelper.invokeMethodAsync('OnDeselectAll');
            resetGraphSelection(nodes, edges);
        }
    });

    container.network = network;
    container.nodesDataSet = nodes;
    container.edgesDataSet = edges;
}

export function updateGraph(container, nodesData, edgesData) {
    if (container && container.nodesDataSet && container.edgesDataSet) {
        originalNodes = JSON.parse(JSON.stringify(nodesData));
        originalEdges = JSON.parse(JSON.stringify(edgesData));
        container.nodesDataSet.clear();
        container.nodesDataSet.add(nodesData);
        container.edgesDataSet.clear();
        container.edgesDataSet.add(edgesData);
    }
}

function filterGraphSelection(nodesDataSet, edgesDataSet, startNodeIds) {
    const connectedNodes = new Set(startNodeIds);
    const connectedEdges = new Set();
    
    const adj = {};
    const revAdj = {};
    originalEdges.forEach(edge => {
        const id = edge.id || `${edge.from}_${edge.to}`;
        if (!adj[edge.from]) adj[edge.from] = [];
        adj[edge.from].push({ to: edge.to, edgeId: id });
        
        if (!revAdj[edge.to]) revAdj[edge.to] = [];
        revAdj[edge.to].push({ from: edge.from, edgeId: id });
    });
    
    // Downstream traversal
    let queue = [...startNodeIds];
    let visited = new Set(startNodeIds);
    while (queue.length > 0) {
        const curr = queue.shift();
        const edgesOut = adj[curr] || [];
        edgesOut.forEach(item => {
            connectedEdges.add(item.edgeId);
            if (!visited.has(item.to)) {
                visited.add(item.to);
                connectedNodes.add(item.to);
                queue.push(item.to);
            }
        });
    }
    
    // Upstream traversal
    queue = [...startNodeIds];
    visited = new Set(startNodeIds);
    while (queue.length > 0) {
        const curr = queue.shift();
        const edgesIn = revAdj[curr] || [];
        edgesIn.forEach(item => {
            connectedEdges.add(item.edgeId);
            if (!visited.has(item.from)) {
                visited.add(item.from);
                connectedNodes.add(item.from);
                queue.push(item.from);
            }
        });
    }
    
    const nodesUpdates = originalNodes.map(node => {
        return {
            id: node.id,
            hidden: !connectedNodes.has(node.id)
        };
    });
    nodesDataSet.update(nodesUpdates);
    
    const edgesUpdates = originalEdges.map(edge => {
        const id = edge.id || `${edge.from}_${edge.to}`;
        return {
            id: id,
            hidden: !connectedEdges.has(id)
        };
    });
    edgesDataSet.update(edgesUpdates);
}

function resetGraphSelection(nodesDataSet, edgesDataSet) {
    const nodesUpdates = originalNodes.map(node => {
        return { id: node.id, hidden: false };
    });
    nodesDataSet.update(nodesUpdates);
    
    const edgesUpdates = originalEdges.map(edge => {
        const id = edge.id || `${edge.from}_${edge.to}`;
        return { id: id, hidden: false };
    });
    edgesDataSet.update(edgesUpdates);
}
