void setup() 
{ 
 Serial.begin(9600); 
 pinMode(2, INPUT); 
} 
 
void loop() 
{ 
 Serial.print(analogRead(A0)); 
 Serial.print(","); 
 Serial.print(analogRead(A1)); 
 Serial.print(","); 
 Serial.println(digitalRead(2)); 
 delay(20); 
} 

