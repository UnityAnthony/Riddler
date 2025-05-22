using UnityEngine;

[CreateAssetMenu(fileName = "Riddle", menuName = "Riddler/Riddle", order = 1)]
public class Riddle : ScriptableObject
{
    public string Question = string.Empty;
    public string Answer = string.Empty;
}
