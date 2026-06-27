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
                face: 'Outfit, Inter, sans-serif'
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

    network.on("selectNode", function (params) {
        if (params.nodes.length > 0) {
            const nodeId = params.nodes[0];
            dotNetHelper.invokeMethodAsync('OnNodeSelected', nodeId);
        }
    });

    container.network = network;
    container.nodesDataSet = nodes;
    container.edgesDataSet = edges;
}

export function updateGraph(container, nodesData, edgesData) {
    if (container && container.nodesDataSet && container.edgesDataSet) {
        container.nodesDataSet.clear();
        container.nodesDataSet.add(nodesData);
        container.edgesDataSet.clear();
        container.edgesDataSet.add(edgesData);
    }
}
