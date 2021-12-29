using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using XMLEngine.GameEngine.Logic;
using XMLEngine.GameEngine.Sprite;
using XMLEngine.GameEngine.Interface;
using XMLEngine.GameFramework.Logic;

public class EditMonster : EditorWindow 
{
	private GameObject MonsterGO = null;
	private int monsterExtensionID = 0;
	private float Speed = 0.30f;
	private GUIContent label1;
	private GUIContent label3;
	private GameObject[] selection = null;
	private string SpeedStr = "1.0";
	
	private bool isError = false;
	private bool createPressed = false;

    [MenuItem("Monster/Edit Monster Speed")]
	static void ShowWindow () 
	{
		EditMonster window = (EditMonster)EditorWindow.GetWindow (typeof (EditMonster));
		window.position = new Rect(Screen.width/2 + 300,400,600,300);
	
	}
	
	void OnEnable() {
		if(!isError)
		{	        
			selection = Selection.gameObjects;			
	    	if(selection.Length == 1)
			{
				string name = (selection[0] as GameObject).name;
				string[] fields = name.Split('_');
				if (2 == fields.Length && "Role" == fields[0])
				{
					int roleID = System.Convert.ToInt32(fields[1]);

                    if (roleID >= SpriteBaseIds.MonsterBaseId && roleID < SpriteBaseIds.PetBaseId)
					{
						
						IObject io = U3DUtils.GetGameObjectOwnerObject(selection[0]);
						if (null != io)
						{
							if (io is GSprite)
							{
								MonsterGO = selection[0];
								monsterExtensionID = (io as GSprite).ExtensionID;
								SpeedStr = U3DUtils.GetAnimationSpeed(MonsterGO).ToString();
							}
						}
					}
					else
					{
						Debug.LogError("Selection Error - Not monster!");
					}
				}
				else
				{
					Debug.LogError("Selection Error - Not monster!");
				}
			}
	    	else if(selection.Length > 1)
				Debug.LogError("Selection Error - Could not get selection : Too many objects selected!");
	    	
	    	
		    label1 = new GUIContent("Monster object to Edit","");
			label3 = new GUIContent("Action Speed","");
	    } 
	}
	
	
	void OnGUI()
	{
		if(!isError)
		{			
			GUILayout.Label ("Configuration", EditorStyles.boldLabel);	
			
			MonsterGO = EditorGUILayout.ObjectField (label1, MonsterGO, typeof(GameObject), true) as GameObject;
			
			EditorGUILayout.LabelField("");

			
			SpeedStr = EditorGUILayout.TextField(SpeedStr);

			EditorGUILayout.LabelField("");

			if(GUILayout.Button("Save Speed"))
			{
				if(MonsterGO != null)
				{
					createPressed = true;

					if(CheckForErrors())
					{
						Speed = (float)System.Convert.ToDouble(SpeedStr);
						StoreData();	
						createPressed = false;
						this.Close();						
					}
					else
					{
						createPressed = false;
					}			
				}
				else
				{
					this.ShowNotification(new GUIContent("Monster Object must be selected."));
					GUIUtility.keyboardControl = 0; 
				}
			}
		}
		else
				EditorGUILayout.LabelField("The Monster Object edit Tool cannot operate in not play mode. Play and reselect.");

				
	}
	
	
	void StoreData()
	{
		SaveToFile(monsterExtensionID, Speed);
		SaveMonsterConfig4Server(monsterExtensionID, Speed);
		U3DUtils.ModifyAnimationSpeed(MonsterGO, Speed);
		if (null != Global.Data && null != Global.Data.GameScene)
		{
			Global.Data.GameScene.ModifyMonstersSpeed(monsterExtensionID, Speed);
		}

		
	}

	public static void SaveToFile(int monsterID, float speed)
	{
		string pathWithName = Application.dataPath + "/StreamingAssets/" + "MonsterSpeed" + ".xml";

		XmlDocument xmlDoc = new XmlDocument();
		XmlElement root = null;
		if (File.Exists(pathWithName))
		{
			xmlDoc.Load(pathWithName);
			root = (XmlElement)xmlDoc.SelectSingleNode("Config");
		}
		else
		{
			XmlDeclaration dec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "");
			xmlDoc.AppendChild(dec);
		
			root = xmlDoc.CreateElement("Config");
			xmlDoc.AppendChild(root);
		}

		bool found = false;
		XmlNodeList nodelist = root.ChildNodes;
		foreach (XmlNode node in nodelist)
		{
			XmlElement xmlelement = (XmlElement)node;
			if (xmlelement.GetAttribute("ID") == monsterID.ToString())
			{
				found = true;
				xmlelement.SetAttribute("Speed", speed.ToString());
				break;
			}
		}

		if (!found)
		{
			XmlElement monster = xmlDoc.CreateElement("Monster");
			monster.SetAttribute("ID", monsterID.ToString());
			monster.SetAttribute("Speed", speed.ToString());
			root.AppendChild(monster);
		}
		
		xmlDoc.Save(pathWithName);
	}

	private string JoinStringArray(string[] arry)
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < arry.Length; i++)
		{
			sb.Append(arry[i]);

			if (i < arry.Length - 1)
			{
				sb.Append(",");
			}
		}

		return sb.ToString();
	}

	private float GetAnimationLenght(string name, float speed)
	{
		Animation ani = MonsterGO.GetComponent<Animation>();
		return ((ani[name].length / speed) * 1000.0f);
	}

	private void SaveMonsterConfig4Server(int monsterID, float speed)
	{
		string pathWithName = Application.dataPath + string.Format("/UIResources/Config/GuaiWu/{0}.xml", ConfigMonsters.GetMonster3DResNameByID(monsterID));
		if (File.Exists(pathWithName))
		{
			File.Delete(pathWithName);
		}
		
		XmlDocument xmlDoc = new XmlDocument();
		XmlElement root = null;

		XmlDeclaration dec = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "");
		xmlDoc.AppendChild(dec);
		
		root = xmlDoc.CreateElement("Config");
		xmlDoc.AppendChild(root);

		XmlElement speedConfig = xmlDoc.CreateElement("SpeedConfig");
		root.AppendChild(speedConfig);

		XmlElement frameConfig = xmlDoc.CreateElement("FrameConfig");
		root.AppendChild(frameConfig);

		speedConfig.SetAttribute("UnitSpeed", "100");

		string[] ticks = { "400", "130", "0", "100", "100", "0", "100", "0", "0", "0", "0", "2000", "100" };
		string[] eachActionFrameRange = { "3", "3", "0", "3", "3", "0", "3", "0", "0", "0", "0", "1", "3" };
		string[] eachActionEffectiveFrame = { "-1", "-1", "-1", "1", "1", "0", "1", "-1", "-1", "-1", "-1", "-1", "-1" };

		ticks[0] = Mathf.FloorToInt(GetAnimationLenght("stand", speed) / Global.SafeConvertToInt32(eachActionFrameRange[0])).ToString();
		ticks[1] = Mathf.FloorToInt(GetAnimationLenght("walk", speed) / Global.SafeConvertToInt32(eachActionFrameRange[1])).ToString();
		ticks[3] = Mathf.FloorToInt(GetAnimationLenght("attack", speed) / Global.SafeConvertToInt32(eachActionFrameRange[3])).ToString();
		
		ticks[6] = Mathf.FloorToInt(GetAnimationLenght("die", speed) / Global.SafeConvertToInt32(eachActionFrameRange[6])).ToString();
		ticks[12] = Mathf.FloorToInt(GetAnimationLenght("hit", speed) / Global.SafeConvertToInt32(eachActionFrameRange[12])).ToString();

		ticks[11] = Mathf.FloorToInt(Mathf.Max(100, 500.0f - GetAnimationLenght("Attack", speed))).ToString();

		speedConfig.SetAttribute("Tick", JoinStringArray(ticks));
		frameConfig.SetAttribute("EachActionFrameRange", JoinStringArray(eachActionFrameRange));
		frameConfig.SetAttribute("EachActionEffectiveFrame", JoinStringArray(eachActionEffectiveFrame));
		
		xmlDoc.Save(pathWithName);
	}
	
	bool CheckForErrors()
	{
		if (null == MonsterGO)
		{
			this.ShowNotification(new GUIContent("Monster GameObject must be selected."));
			return false;
		}

		return true;
	}	
}