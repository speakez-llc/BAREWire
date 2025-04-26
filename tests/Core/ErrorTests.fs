module BAREWire.Tests.Core

open Expecto
open BAREWire.Core
open BAREWire.Core.Error

[<Tests>]
let errorTests =
    testList "Error Module Tests" [
        testCase "validationError creates SchemaValidationError" <| fun _ ->
            let error = validationError "Invalid schema"
            match error with
            | SchemaValidationError msg -> Expect.equal msg "Invalid schema" "Message should match"
            | _ -> failtest "Expected SchemaValidationError"
            
        testCase "decodingError creates DecodingError" <| fun _ ->
            let error = decodingError "Failed to decode"
            match error with
            | DecodingError msg -> Expect.equal msg "Failed to decode" "Message should match"
            | _ -> failtest "Expected DecodingError"
            
        testCase "encodingError creates EncodingError" <| fun _ ->
            let error = encodingError "Failed to encode"
            match error with
            | EncodingError msg -> Expect.equal msg "Failed to encode" "Message should match"
            | _ -> failtest "Expected EncodingError"
            
        testCase "typeMismatchError creates TypeMismatchError" <| fun _ ->
            let expected = Type.Primitive PrimitiveType.U32
            let actual = Type.Primitive PrimitiveType.I32
            let error = typeMismatchError expected actual
            match error with
            | TypeMismatchError(e, a) -> 
                Expect.equal e expected "Expected type should match"
                Expect.equal a actual "Actual type should match"
            | _ -> failtest "Expected TypeMismatchError"
            
        testCase "outOfBoundsError creates OutOfBoundsError" <| fun _ ->
            let offset = 10 * 1<offset>
            let length = 5 * 1<bytes>
            let error = outOfBoundsError offset length
            match error with
            | OutOfBoundsError(o, l) -> 
                Expect.equal o offset "Offset should match"
                Expect.equal l length "Length should match"
            | _ -> failtest "Expected OutOfBoundsError"
            
        testCase "invalidValueError creates InvalidValueError" <| fun _ ->
            let error = invalidValueError "Invalid value"
            match error with
            | InvalidValueError msg -> Expect.equal msg "Invalid value" "Message should match"
            | _ -> failtest "Expected InvalidValueError"
            
        testCase "toString converts error to string" <| fun _ ->
            let error1 = validationError "Invalid schema"
            let error2 = decodingError "Failed to decode"
            let error3 = encodingError "Failed to encode"
            let error4 = typeMismatchError (Type.Primitive PrimitiveType.U32) (Type.Primitive PrimitiveType.I32)
            let error5 = outOfBoundsError (10 * 1<offset>) (5 * 1<bytes>)
            let error6 = invalidValueError "Invalid value"
            
            Expect.stringContains (toString error1) "Schema validation error: Invalid schema" "SchemaValidationError string should contain message"
            Expect.stringContains (toString error2) "Decoding error: Failed to decode" "DecodingError string should contain message"
            Expect.stringContains (toString error3) "Encoding error: Failed to encode" "EncodingError string should contain message"
            Expect.stringContains (toString error4) "Type mismatch" "TypeMismatchError string should describe mismatch"
            Expect.stringContains (toString error5) "Out of bounds" "OutOfBoundsError string should describe bounds issue"
            Expect.stringContains (toString error6) "Invalid value: Invalid value" "InvalidValueError string should contain message"
            
        testCase "map transforms successful result" <| fun _ ->
            let result = Ok 5
            let mapped = map (fun x -> x * 2) result
            Expect.equal mapped (Ok 10) "map should transform value in Ok case"
            
        testCase "map preserves error in error case" <| fun _ ->
            let result = Error (validationError "Invalid schema")
            let mapped = map (fun x -> x * 2) result
            Expect.equal mapped result "map should preserve error in Error case"
            
        testCase "bind transforms successful result" <| fun _ ->
            let result = Ok 5
            let bound = bind (fun x -> Ok (x * 2)) result
            Expect.equal bound (Ok 10) "bind should apply function to value in Ok case"
            
        testCase "bind preserves error in error case" <| fun _ ->
            let result = Error (validationError "Invalid schema")
            let bound = bind (fun x -> Ok (x * 2)) result
            Expect.equal bound result "bind should preserve error in Error case"
            
        testCase "mapError transforms error" <| fun _ ->
            let result = Error (validationError "Invalid schema")
            let mapped = mapError (fun _ -> encodingError "New error") result
            Expect.equal mapped (Error (encodingError "New error")) "mapError should transform error in Error case"
            
        testCase "mapError preserves value in success case" <| fun _ ->
            let result = Ok 5
            let mapped = mapError (fun _ -> encodingError "New error") result
            Expect.equal mapped result "mapError should preserve value in Ok case"
            
        testCase "ofValidation converts validation result to Error.Result" <| fun _ ->
            let validResult = Ok "Valid"
            let invalidResult = Error ["Error 1"; "Error 2"]
            
            let result1 = ofValidation validResult
            let result2 = ofValidation invalidResult
            
            Expect.equal result1 (Ok "Valid") "Valid result should be preserved"
            match result2 with
            | Error (SchemaValidationError msg) -> 
                Expect.stringContains msg "Error 1" "Error message should contain first validation error"
                Expect.stringContains msg "Error 2" "Error message should contain second validation error"
            | _ -> failtest "Expected SchemaValidationError"
    ]