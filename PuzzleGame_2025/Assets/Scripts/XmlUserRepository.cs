
using System.IO;
using System.Xml;
using UnityEngine;

public class XmlUserRepository
{
    //Като член-данна ще пазя пълния път до файла
    private string filePath;

    //Конструктор
    public XmlUserRepository(string fileName = "users.xml")
    {
        //Сглобява правилен път от папката за записване да файлове, които да не се трият след рестарт и името на файла
        //C:\Users\Aylin\AppData\LocalLow\CompanyName\ProductName\users.xml

        filePath = Path.Combine(Application.persistentDataPath, fileName);

        //Подсигуряваме, че такъв файл съществува
        CreateFileIfItDoesntExist();
       

    }

    private void CreateFileIfItDoesntExist()
    {
        //Проверявам дали има вече файл
        if (File.Exists(filePath))
        {
            return;
        }

        //Създавам ново DOM дърво, празно
        var doc = new XmlDocument();

        //Създавам декларация, с която ще започва моя файл
        //<?xml version="1.0" encoding="utf-8"?>
      
        var declaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        
        //Добавяме декларацията към дървото, но тя не е Node
        doc.AppendChild(declaration);

        //Създаваме корена на дървото, може да имаме само един корен и го добавяме към дървото
        var root = doc.CreateElement("Users");
        doc.AppendChild(root);

        //Запазваме дървото във xml файл
        doc.Save(filePath);

        //Файлът ще бъде празен ияе изглежда така
        //<?xml version="1.0" encoding="utf-8"?>
        //< Users />

    }

    //Функция, която зарежда документа от диска в паметта
    private XmlDocument LoadDoc()
    {
        var doc = new XmlDocument();
        doc.Load(filePath);
        return doc;
    }

    // Проверка дали този потребител вече съществува
    public bool UserExists(string username)
    {
       
        if (username == null || username.Length == 0)
        {
            return false;
        }

        var doc = LoadDoc();

       
        // Използвам XPath израз за намиране на елемент с атрибут username равен на подадения username
        var node = doc.SelectSingleNode($"/Users/User[@username=\"{username}\"]");

        //Ако намери такъв възел връща true, ако не - връща false
        if (node == null ){

            return false;
        }
        else
        {
            return true;
        }
    }

    //Функция, която проверява дали има такъв потрбител с такава парола
    public bool ValidateLogin(string username, string password)
    {
        
        var doc = LoadDoc();

        var node = doc.SelectSingleNode(
            $"/Users/User[@username=\"{username}\"][@password=\"{password}\"]"
        );

        if (node == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //Функция за регистрация на потребител
    public bool TryRegister(string username, string password, ref string message)
    {
       

        var doc = LoadDoc();
        //Проверявам има ли вече такъв потребител
        var existingUser = doc.SelectSingleNode($"/Users/User[@username=\"{username}\"]");
        if (existingUser != null)
        {
            message = "A user with this username already exists.";
            return false;
        }

        //Създавам нов елемент
        var user = doc.CreateElement("User");

        //Слагам му съответните атрибути
        user.SetAttribute("username", username);
        user.SetAttribute("password", password);

        var statistic_node = doc.CreateElement("Stats");
        statistic_node.SetAttribute("totalPoints", "0");

        user.AppendChild(statistic_node);

        var progress_node = doc.CreateElement("Progress");

        var picture_progress_1 = doc.CreateElement("PictureProgress");
        picture_progress_1.SetAttribute("pictureId", "prehistoric");
        picture_progress_1.SetAttribute("unlockedUpTo", "1");
        progress_node.AppendChild(picture_progress_1);

        var picture_progress_2 = doc.CreateElement("PictureProgress");
        picture_progress_2.SetAttribute("pictureId", "egypt");
        picture_progress_2.SetAttribute("unlockedUpTo", "1");
        progress_node.AppendChild(picture_progress_2);

        var picture_progress_3 = doc.CreateElement("PictureProgress");
        picture_progress_3.SetAttribute("pictureId", "knights");
        picture_progress_3.SetAttribute("unlockedUpTo", "1");
        progress_node.AppendChild(picture_progress_3);

        var picture_progress_4 = doc.CreateElement("PictureProgress");
        picture_progress_4.SetAttribute("pictureId", "future");
        picture_progress_4.SetAttribute("unlockedUpTo", "1");
        progress_node.AppendChild(picture_progress_4);

        user.AppendChild(progress_node);

        //Добавям го като следващо дете на корена
        doc.DocumentElement.AppendChild(user);
        doc.Save(filePath);

        message = "Registration successful! Please return to the login page.";
        return true;
    }

    public UserInfoClass LoadUserInfo(string username)
    {
        if(username == null || username.Length == 0)
        {
            return null;
        }

        var doc = LoadDoc();
        var node = doc.SelectSingleNode(
            $"/Users/User[@username=\"{username}\"]"
        );


        UserInfoClass user = new UserInfoClass();
        user.username = username;
        

        var statsNode = node.SelectSingleNode("Stats");
        user.totalPoints = statsNode.Attributes[0].Value;

        var progressNode = node.SelectSingleNode("Progress");
        foreach(XmlNode picture in progressNode.SelectNodes("PictureProgress"))
        {
            string pictureId = picture.Attributes[0].Value;
            string unlockedUpTo = picture.Attributes[1].Value;
            user.unlockedUpTo[pictureId]= unlockedUpTo;
        }
        
        return user;
    }
    public void AddPoints(string username, int newPoints)
    {
        var doc = LoadDoc();
        var node = doc.SelectSingleNode(
            $"/Users/User[@username=\"{username}\"]"
        ) as XmlElement;
       
        var statsNode = node.SelectSingleNode("Stats") as XmlElement;
        statsNode.SetAttribute("totalPoints", newPoints.ToString());
        doc.Save(filePath);

    }
    public void updateLevelLocking(string username, int unlockedUpToNew, string pictureId)
    {
        var doc = LoadDoc();
       
        var pictureProgress = doc.SelectSingleNode(
            $"/Users/User[@username=\"{username}\"]/Progress/PictureProgress[@pictureId=\"{pictureId}\"]"
               ) as XmlElement;

        pictureProgress.SetAttribute("unlockedUpTo", unlockedUpToNew.ToString());
        doc.Save(filePath);

    }

}
