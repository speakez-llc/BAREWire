namespace BAREWire.Core

/// <summary>
/// Error types used throughout BAREWire
/// </summary>
module Error =
    /// <summary>
    /// Errors that can occur during BAREWire operations
    /// </summary>
    type Error =
        | SchemaValidationError of message:string
        | DecodingError of message:string
        | EncodingError of message:string
        | TypeMismatchError of expected:Type * actual:Type
        | OutOfBoundsError of offset:int<offset> * length:int<bytes>
        | InvalidValueError of message:string

    /// <summary>
    /// Result type for BAREWire operations
    /// </summary>
    type Result<'T> = Result<'T, Error>
    
    /// <summary>
    /// Creates a schema validation error with the specified message
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A SchemaValidationError</returns>
    let validationError message = SchemaValidationError message
    
    /// <summary>
    /// Creates a decoding error with the specified message
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A DecodingError</returns>
    let decodingError message = DecodingError message
    
    /// <summary>
    /// Creates an encoding error with the specified message
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An EncodingError</returns>
    let encodingError message = EncodingError message
    
    /// <summary>
    /// Creates a type mismatch error with expected and actual types
    /// </summary>
    /// <param name="expected">The expected type</param>
    /// <param name="actual">The actual type</param>
    /// <returns>A TypeMismatchError</returns>
    let typeMismatchError expected actual = TypeMismatchError(expected, actual)
    
    /// <summary>
    /// Creates an out of bounds error with the specified offset and length
    /// </summary>
    /// <param name="offset">The offset that caused the error</param>
    /// <param name="length">The buffer length</param>
    /// <returns>An OutOfBoundsError</returns>
    let outOfBoundsError offset length = OutOfBoundsError(offset, length)
    
    /// <summary>
    /// Creates an invalid value error with the specified message
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>An InvalidValueError</returns>
    let invalidValueError message = InvalidValueError message
    
    /// <summary>
    /// Converts an error to a human-readable string
    /// </summary>
    /// <param name="error">The error to convert</param>
    /// <returns>A string representation of the error</returns>
    let toString error =
        match error with
        | SchemaValidationError message -> $"Schema validation error: {message}"
        | DecodingError message -> $"Decoding error: {message}"
        | EncodingError message -> $"Encoding error: {message}"
        | TypeMismatchError(expected, actual) -> $"Type mismatch: expected {expected}, got {actual}"
        | OutOfBoundsError(offset, length) -> $"Out of bounds: offset {offset}, length {length}"
        | InvalidValueError message -> $"Invalid value: {message}"
        
    /// <summary>
    /// Handles an error by converting it to a string
    /// </summary>
    /// <param name="error">The error to handle</param>
    /// <returns>A string representation of the error</returns>
    let handle error = toString error
    
    /// <summary>
    /// Maps a successful result using the specified function
    /// </summary>
    /// <param name="f">The mapping function</param>
    /// <param name="result">The result to map</param>
    /// <returns>A new result with the mapped value</returns>
    let map f result =
        match result with
        | Ok value -> Ok (f value)
        | Error error -> Error error
        
    /// <summary>
    /// Binds a successful result to the specified function
    /// </summary>
    /// <param name="f">The binding function</param>
    /// <param name="result">The result to bind</param>
    /// <returns>The result of applying the function to the value</returns>
    let bind f result =
        match result with
        | Ok value -> f value
        | Error error -> Error error
        
    /// <summary>
    /// Maps the error in a failed result using the specified function
    /// </summary>
    /// <param name="f">The error mapping function</param>
    /// <param name="result">The result to map</param>
    /// <returns>A new result with the mapped error</returns>
    let mapError f result =
        match result with
        | Ok value -> Ok value
        | Error error -> Error (f error)
        
    /// <summary>
    /// Converts a validation result to an Error.Result
    /// </summary>
    /// <param name="result">The validation result</param>
    /// <returns>The corresponding Error.Result</returns>
    let ofValidation result =
        match result with
        | Ok value -> Ok value
        | Error errors -> 
            let message = String.concat "; " errors
            Error (validationError message)