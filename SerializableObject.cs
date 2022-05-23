using UnityEngine;

namespace DonutDiner.Framework.Data
{
    // Abstract class to allow for object serialization.
    // Inherit SerializableObject instead of Monobehavior, and optionally override functions to save more data.

    // Implemented by Ben Russell

    public abstract class SerializableObject : MonoBehaviour
    {
        #region Fields

        // Global Unique ID for identifying object when serializing
        [HideInInspector] [SerializeField] private string _guid = "";

        // Flag for if object has loaded its data from a save file
        private bool _loadedFromFile = false;

        #endregion

        #region Unity Methods

#if UNITY_EDITOR

        private void Reset()
        {
            // Generate GUID during normal object creation
            GenerateGuid();
        }

        private void OnValidate()
        {
            if (_guid == "")
            {
                // Check guid has been created (In case object was already created when inheritance was switched to Serializable Object)
                GenerateGuid();
            }

            if (!Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(transform.root.gameObject.scene.name))
                {
                    // Add membership with Object Manager if needed
                    AddToObjectManager();
                }
            }
        }

#endif

        #endregion

        #region Public Methods

        public string GetGuid() => _guid;

        public bool GetLoadedFromFile() => _loadedFromFile;

        public virtual void WriteData()
        {
            // Only generic data by default
            WriteHeader();
        }

        public virtual void ReadData()
        {
            // Only generic data by default
            ReadHeader();
        }

        public void AddToObjectManager()
        {
            if (ObjectManager.Instance().AddSerializableObject(this))
            {
                // GUID was already taken, (because I come from a duplication)
                GenerateGuid();
            }
        }

        #endregion

        #region Private Methods

        private void GenerateGuid() => _guid = System.Guid.NewGuid().ToString();

        // Write attributes common to all serializable objects
        protected void WriteHeader()
        {
            Serialize.Write(gameObject.name); // For save-file readability only
            Serialize.Write(GetGuid());
            Serialize.Write(transform.position);
            Serialize.Write(transform.rotation.eulerAngles);
        }

        // Load attributes common to all serializable objects
        protected void ReadHeader()
        {
            _loadedFromFile = true;

            // _guid has already been read, in order to find this unique object
            transform.position = Serialize.ReadVector3();
            transform.rotation = Quaternion.Euler(Serialize.ReadVector3());
        }

        #endregion
    }
}