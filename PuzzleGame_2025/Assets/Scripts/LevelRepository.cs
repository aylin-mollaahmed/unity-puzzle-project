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
        config.piecePoint = int.Parse(node.Attributes["piecePoint"].Value);
        
        var helpNode = node.SelectSingleNode("Help") as XmlElement;

        var helpConfig = new HelpConfig();

        if (helpNode != null)
        {
            helpConfig.enabled = bool.Parse(helpNode.GetAttribute("enabled"));
            helpConfig.maxUses = int.Parse(helpNode.GetAttribute("maxUses"));
            helpConfig.costPoints = int.Parse(helpNode.GetAttribute("costPoints"));
        }
        else
        {
            // Default ако няма Help в XML
            helpConfig.enabled = false;
            helpConfig.maxUses = 0;
            helpConfig.costPoints = 0;
        }

        config.help = helpConfig;

        return config;
    }

}
