Managing MQTT connection : 
    - OpenBve/Mqtt/Mqtt.cs => MQTT client initialisation and management of received messages linked to actuators
    - OpenBve/OldCode/TrainManager.cs => where most of the publishing of sensors data is done
    - MQTT topics : train/sensors/... or train/actuators/...

Use Visual Studio Code to build the project
Don't forget to set a valid adress for the MQTT broker => the simulator crashes if not connected to a functionning MQTT broker