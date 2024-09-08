namespace SharperAppImages;

public class UnexpectedAppImageExecutionCode(int code, string standardError, string standardOut) 
    : IOException($"""
                   Unexpected extraction code: {code}.

                   Error output: {standardError}

                   Log output: {standardOut}
                   """);