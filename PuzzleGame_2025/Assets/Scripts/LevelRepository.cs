using System.Xml;
using UnityEngine;

public class LevelRepository
{
    private readonly XmlDocument doc;

    public LevelRepository(TextAsset xmlFile)
    {
        doc = new XmlDocument();
        doc.LoadXml(xmlFile.text);
    }

    public LevelConfig GetConfigByDifficulty(int difficultyId)
    {
        var node = doc.SelectSingleNode($"/GameConfig/Difficulties/Difficulty[@id='{difficultyId}']");
        if (node == null)
        {
            Debug.LogError($"No difficulty with id={difficultyId} in XML!");
            return null;
        }

        var config = new LevelConfig();
        config.pieceShape = node.Attributes["pieceShape"].Value;
        config.randomOrientation = bool.Parse(node.Attributes["randomOrientation"].Value);
        config.piecesCount = int.Parse(node.Attributes["shortSidePieces"].Value);

        return config;
    }
}
