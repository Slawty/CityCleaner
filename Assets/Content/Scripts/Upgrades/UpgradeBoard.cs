using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UpgradeBoard : MonoBehaviour
{
    [SerializeField] UpgradeConnectionLine connectionLinePrefab;
    [SerializeField] Transform connectionLineRoot;

    readonly List<UpgradeNode> nodes = new();
    readonly List<UpgradeConnectionLine> connectionLines = new();

    public IReadOnlyList<UpgradeNode> Nodes => nodes;

    void Awake()
    {
        CollectNodes();
        BuildConnections();
    }

    public void CollectNodes()
    {
        nodes.Clear();
        nodes.AddRange(GetComponentsInChildren<UpgradeNode>(true));
    }

    public void BuildConnections()
    {
        ClearConnections();
        ClearConnectionLineRoot();

        if (connectionLinePrefab == null)
            return;

        Transform lineParent = connectionLineRoot != null ? connectionLineRoot : transform;

        foreach (UpgradeNode node in nodes)
        {
            if (node.Prerequisites == null)
                continue;

            foreach (UpgradeNode prerequisite in node.Prerequisites)
            {
                if (prerequisite == null)
                    continue;

                UpgradeConnectionLine line = Instantiate(connectionLinePrefab, lineParent);
                line.Bind(prerequisite.LineAnchor, node.LineAnchor);
                line.SetVisible(false);
                connectionLines.Add(line);
            }
        }
    }

    public void RefreshConnectionsVisibility()
    {
        int lineIndex = 0;

        foreach (UpgradeNode node in nodes)
        {
            if (node.Prerequisites == null)
                continue;

            foreach (UpgradeNode prerequisite in node.Prerequisites)
            {
                if (prerequisite == null || lineIndex >= connectionLines.Count)
                    continue;

                bool visible = Managers.Upgrades.IsRevealed(prerequisite) && Managers.Upgrades.IsRevealed(node);
                connectionLines[lineIndex].SetVisible(visible);
                lineIndex++;
            }
        }
    }

    public void RefreshAllViews(UpgradeNode selectedNode)
    {
        foreach (UpgradeNode node in nodes)
        {
            UpgradeNodeView view = node.GetComponent<UpgradeNodeView>();
            if (view == null)
                continue;

            int rank = Managers.Upgrades.GetRank(node);
            UpgradeNodeState state = Managers.Upgrades.GetState(node);
            int maxRanks = node.Data != null ? node.Data.maxRanks : 0;
            view.Refresh(state, rank, maxRanks, node == selectedNode);
        }

        RefreshConnectionsVisibility();
    }

    void ClearConnectionLineRoot()
    {
        Transform lineParent = connectionLineRoot != null ? connectionLineRoot : transform;
        if (lineParent == null)
            return;

        UpgradeConnectionLine[] existingLines = lineParent.GetComponentsInChildren<UpgradeConnectionLine>(true);
        foreach (UpgradeConnectionLine existingLine in existingLines)
        {
            if (existingLine == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existingLine.gameObject);
            else
#endif
                Destroy(existingLine.gameObject);
        }
    }

    void ClearConnections()
    {
        foreach (UpgradeConnectionLine line in connectionLines)
        {
            if (line == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(line.gameObject);
            else
#endif
                Destroy(line.gameObject);
        }

        connectionLines.Clear();
    }

#if UNITY_EDITOR
    public void EditorRebuildConnections()
    {
        CollectNodes();
        ClearConnections();
        ClearConnectionLineRoot();

        if (connectionLinePrefab == null)
            return;

        Transform lineParent = connectionLineRoot != null ? connectionLineRoot : transform;

        foreach (UpgradeNode node in nodes)
        {
            if (node.Prerequisites == null)
                continue;

            foreach (UpgradeNode prerequisite in node.Prerequisites)
            {
                if (prerequisite == null)
                    continue;

                GameObject lineObject = (GameObject)PrefabUtility.InstantiatePrefab(connectionLinePrefab.gameObject, lineParent);
                UpgradeConnectionLine line = lineObject.GetComponent<UpgradeConnectionLine>();
                line.Bind(prerequisite.LineAnchor, node.LineAnchor);
                connectionLines.Add(line);
            }
        }

        EditorUtility.SetDirty(this);
    }
#endif
}
