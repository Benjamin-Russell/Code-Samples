using DonutDiner.Framework.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DonutDiner.Framework
{
    // Class for indexing of important game objects and efficient access.
    // One per scene, is not persistent.

    // First implemented by Ben Russell

    public class ObjectManager : MonoBehaviour
    {
        #region Fields

        public List<SerializableObject> SerializedObjects = null;
        public CharacterController PlayerController;

        #endregion

        #region Unity Methods

#if UNITY_EDITOR

        private void Reset()
        {
            // On creation in editor
            SerializedObjects = new List<SerializableObject>();

            // Add objects present in the scene
            SerializableObject[] serializableObjects = FindObjectsOfType<SerializableObject>();

            foreach (var serializableObject in serializableObjects)
            {
                if (!string.IsNullOrEmpty(serializableObject.transform.root.gameObject.scene.name))
                {
                    serializableObject.AddToObjectManager();
                }
            }
        }

#endif

        private void Start()
        {
            GameManager.Instance().ObjectManager = this;
            PlayerController = GameObject.FindGameObjectWithTag("Player").GetComponent<CharacterController>();

            if (Application.isEditor)
            {
                // Sanity check to make sure there are no duplicate GUID's
                for (int i = 0; i < SerializedObjects.Count; ++i)
                {
                    for (int j = i; j < SerializedObjects.Count; ++j)
                    {
                        if (i != j
                            && SerializedObjects[i].GetGuid() == SerializedObjects[j].GetGuid())
                        {
                            Debug.LogWarning(SerializedObjects[i].gameObject.name + " and " + SerializedObjects[j].gameObject.name + " share a GUID! Please alert Programming (Ben)");
                        }
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public static ObjectManager Instance()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ObjectManager objectManager = FindObjectOfType<ObjectManager>();

                if (objectManager)
                {
                    return objectManager;
                }
            }
#endif
            return GameManager.Instance().ObjectManager;
        }

        public bool AddSerializableObject(SerializableObject serializableObject)
        {
            bool guidAlreadyPresent = false;

            // Traverse list backwards to allow for easy cleaning of null references
            for (int i = SerializedObjects.Count - 1; i >= 0; i--)
            {
                if (SerializedObjects[i] == null)
                {
                    // Clean up null reference
                    SerializedObjects.RemoveAt(i);
                }
                else
                {
                    if (SerializedObjects[i] == serializableObject)
                    {
                        // Object already in list, nothing more to do
                        return false;
                    }

                    if (SerializedObjects[i].GetGuid() == serializableObject.GetGuid())
                    {
                        guidAlreadyPresent = true;
                    }
                }
            }

            // Add new object
            SerializedObjects.Add(serializableObject);

            // Return whether object needs a new GUID. (Because it's a duplicated object)
            return guidAlreadyPresent;
        }

        public SerializableObject GetObjectByGuid(string guid)
        {
            foreach (SerializableObject serializableObject in SerializedObjects)
            {
                if (serializableObject.GetGuid() == guid)
                {
                    return serializableObject;
                }
            }

            // Not found!
            return null;
        }

        public void DestroyUnserializedObjects()
        {
            // Destroy Serialized Objects not found in save file (They weren't around to be saved)
            for (int i = SerializedObjects.Count - 1; i >= 0; i--)
            {
                if (!SerializedObjects[i].GetLoadedFromFile())
                {
                    Destroy(SerializedObjects[i].gameObject);
                    SerializedObjects.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}