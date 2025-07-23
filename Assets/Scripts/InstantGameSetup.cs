using UnityEngine;

public class InstantGameSetup : MonoBehaviour
{
    [ContextMenu("Setup Game Now")]
    void SetupGame()
    {
        // Create Player
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0, 1, -5);
        player.GetComponent<Renderer>().material.color = Color.blue;
        
        // Create Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 20);
        
        // Create Gates
        for (int i = 0; i < 3; i++)
        {
            var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = $"MultiplierGate_{i + 1}";
            gate.transform.position = new Vector3(0, 1, i * 8 + 5);
            gate.transform.localScale = new Vector3(4, 2, 0.5f);
            gate.GetComponent<Renderer>().material.color = Color.green;
        }
        
        Debug.Log("✅ Basic game setup complete! Now you can play!");
    }
}
